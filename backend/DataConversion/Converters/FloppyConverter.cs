
using System.Diagnostics;
using System.Text.RegularExpressions;
using asec.Compatibility.EaasApi.Models;
using asec.LongRunning;
using asec.Models.Digitalization;

namespace asec.DataConversion.Converters;

public class FloppyConverterConfig : ConverterConfig
{
    public string GWPath { get; set; }
    public override IConverter ConstructConverter()
    {
        return new FloppyConverter(this);
    }
}

public class FloppyConverter : IConverter
{
    private readonly Regex ToolVersionRegex = new Regex(@"^Host Tools: (\d+).(\d+)$");

    private FloppyConverterConfig _config;

    public FloppyConverter(FloppyConverterConfig config)
    {
        _config = config;
        Version = GetGWVersion();
    }

    private string GetGWVersion()
    {
        if (!Path.Exists(_config.GWPath))
            return "N/A";
        ProcessStartInfo gwProcess = new(_config.GWPath);
        gwProcess.ArgumentList.Add("info");
        gwProcess.RedirectStandardError = true;
        gwProcess.RedirectStandardOutput = true;
        var process = System.Diagnostics.Process.Start(gwProcess)!;
        process.WaitForExit();
        var versionLine = process.StandardError.ReadLine();
        if (versionLine == null)
            return "N/A";
        var versionMatch = ToolVersionRegex.Match(versionLine);
        return versionMatch.Groups[1].Value;
    }

    public Guid Id { get; set; }

    public string Name => "Greaseweazle Floppy Converter";

    public string Version { get; private set; }

    public string Environment
    {
        get
        {
            string OS = System.Environment.OSVersion.VersionString;
            string CLR = System.Environment.Version.ToString();
            return $"OS:{OS},CLR:{CLR}";
        }
    }

    public IList<ArtefactType> SupportedArtefactTypes => new List<ArtefactType>{
        ArtefactType.SfmFloppy
    };

    public async Task<ConversionResult> Start(Process process, CancellationToken cancellationToken)
    {
        if (process.Artefacts.Any(a => a.Type != ArtefactType.SfmFloppy))
        {
            process.Status = ProcessStatus.Failed;
            process.StatusDetail = "InvalidInput";
            return null;
        }
        using var logStream = new FileStream(process.LogPath, FileMode.Append);
        using var logWriter = new StreamWriter(logStream);
        logWriter.AutoFlush = true;

        void logCallback(object sender, DataReceivedEventArgs e)
        {
            logWriter.WriteLine(e.Data);
        }

        process.Status = ProcessStatus.Running;

        List<ConvertedFile> outFiles = new();

        foreach (Artefact a in process.Artefacts)
        {
            logWriter.WriteLine("Converting artefact: " + a.Id.ToString());
            string sfmFile = await process.FetchArtefact(a, false, cancellationToken);
            string imgFile = Path.Combine(process.OutputDir, Path.GetFileNameWithoutExtension(sfmFile) + ".img");

            ProcessStartInfo gwProcessInfo = new ProcessStartInfo(_config.GWPath)
            {
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };
            gwProcessInfo.ArgumentList.Add("convert");
            gwProcessInfo.ArgumentList.Add("--format");
            gwProcessInfo.ArgumentList.Add("ibm.scan");
            gwProcessInfo.ArgumentList.Add(sfmFile);
            gwProcessInfo.ArgumentList.Add(imgFile);
            var gwProcess = System.Diagnostics.Process.Start(gwProcessInfo) ?? throw new Exception("Failed to start process");
            gwProcess.OutputDataReceived += logCallback;
            gwProcess.ErrorDataReceived += logCallback;
            gwProcess.BeginOutputReadLine();
            gwProcess.BeginErrorReadLine();
            await gwProcess.WaitForExitAsync(cancellationToken);
            if (!gwProcess.HasExited || gwProcess.ExitCode != 0)
            {
                // TODO: check other failure modes
                process.Status = ProcessStatus.Failed;
                process.StatusDetail = StatusDetail.FailedToConvertArtefact.ToString();
                return null;
            }

            outFiles.Add(new(imgFile, DeviceID.FromQID(DeviceID.Floppy)));
        }
        process.Status = ProcessStatus.Success;
        return new(outFiles);
    }

    private enum StatusDetail
    {
        FailedToConvertArtefact
    }
}