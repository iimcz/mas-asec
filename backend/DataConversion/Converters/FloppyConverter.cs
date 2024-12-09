
using System.Diagnostics;
using System.Text.RegularExpressions;
using asec.Compatibility.EaasApi.Models;
using asec.LongRunning;
using asec.Models.Digitalization;

namespace asec.DataConversion.Converters;

public class FloppyConverterConfig : ConverterConfig
{
    public string GWPath { get; set; }
    public string ConversionFormat { get; set; }
    public string ConversionSuffix { get; set; }
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
        if (!versionMatch.Success)
            return "N/A";
        return $"{versionMatch.Groups[1].Value}.{versionMatch.Groups[2].Value}";
    }

    public Guid Id { get; set; }

    public string Name => "Greaseweazle Floppy Converter";

    public string Version { get; private set; }

    public string Environment
    {
        get
        {
            // TODO: maybe separate non-pure-environmental variables, like target format, elsewhere
            string OS = System.Environment.OSVersion.VersionString;
            string CLR = System.Environment.Version.ToString();
            string TRF = _config.ConversionFormat;
            string TRS = _config.ConversionSuffix;
            return $"OS:{OS},CLR:{CLR},TRF:{TRF},TRS:{TRS}";
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
        int copy = 0;

        foreach (Artefact a in process.Artefacts)
        {
            logWriter.WriteLine($"Converting artefact: {a.Id} (name: {a.Name})");
            string sfmFile = await process.FetchArtefact(a, false, cancellationToken);
            string outFile = Path.Combine(process.OutputDir, Path.GetFileNameWithoutExtension(sfmFile) + _config.ConversionSuffix);

            while (File.Exists(outFile))
            {
                outFile = outFile.Substring(0, outFile.Length - _config.ConversionSuffix.Length) + copy + _config.ConversionSuffix;
                copy++;
            }

            ProcessStartInfo gwProcessInfo = new ProcessStartInfo(_config.GWPath)
            {
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };
            gwProcessInfo.ArgumentList.Add("convert");
            gwProcessInfo.ArgumentList.Add("--format");
            gwProcessInfo.ArgumentList.Add(_config.ConversionFormat);
            gwProcessInfo.ArgumentList.Add(sfmFile);
            gwProcessInfo.ArgumentList.Add(outFile);
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

            outFiles.Add(new(outFile, DeviceID.FromQID(DeviceID.Floppy)));
        }

        logWriter.WriteLine("Conversion process finished successfully.");

        process.Status = ProcessStatus.Success;
        return new(outFiles);
    }

    private enum StatusDetail
    {
        FailedToConvertArtefact
    }
}