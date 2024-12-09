using System.Diagnostics;
using System.Text.RegularExpressions;
using asec.Compatibility.EaasApi.Models;
using asec.LongRunning;
using asec.Models.Digitalization;

namespace asec.DataConversion.Converters;

public class AudioTapeConverterConfig : ConverterConfig
{
    public string Audio2TapePath { get; set; }

    public override IConverter ConstructConverter()
    {
        return new AudioTapeConverter(this);
    }
}

public class AudioTapeConverter : IConverter
{
    private readonly Regex ToolVersionRegex = new Regex(@"^audio2tape (fuse-utils) (\d+).(\d+).(\d+)");

    public Guid Id { get; set; }

    public string Name => "Fuse Utils Audio to Tape Converter";

    public string Version { get; private set; }

    public string Environment {
        get {
            // TODO: maybe separate non-pure-environmental variables, like target format, elsewhere
            string OS = System.Environment.OSVersion.VersionString;
            string CLR = System.Environment.Version.ToString();
            return $"OS:{OS},CLR:{CLR}";
        }
    }

    public IList<ArtefactType> SupportedArtefactTypes => new List<ArtefactType>() {
        ArtefactType.WavAudio
    };

    private AudioTapeConverterConfig _config;

    public AudioTapeConverter(AudioTapeConverterConfig config)
    {
        _config = config;
        Version = GetFuseUtilsVersion();
    }

    private string GetFuseUtilsVersion()
    {
        if (Path.Exists(_config.Audio2TapePath))
            return "N/A";
        var startInfo = new ProcessStartInfo(_config.Audio2TapePath) {
            RedirectStandardOutput = true,
            Arguments = "--version"
        };
        var process = System.Diagnostics.Process.Start(startInfo)!;
        process.WaitForExit();
        var versionLine = process.StandardOutput.ReadLine();
        if (versionLine == null)
            return "N/A";
        var match = ToolVersionRegex.Match(versionLine);
        if (!match.Success)
            return "N/A";
        return $"{match.Groups[1].Value}.{match.Groups[2].Value}.{match.Groups[3].Value}";
    }

    public async Task<ConversionResult> Start(Process process, CancellationToken cancellationToken)
    {
        if (process.Artefacts.Any(a => a.Type != ArtefactType.WavAudio))
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
        string outFileSuffix = ".tap";
        int copy = 0;

        foreach (Artefact a in process.Artefacts)
        {
            logWriter.WriteLine($"Converting artefact: {a.Id} (name: {a.Name})");
            string wavFile = await process.FetchArtefact(a, false, cancellationToken);
            string outFile = Path.Combine(process.OutputDir, Path.GetFileNameWithoutExtension(wavFile) + outFileSuffix);

            while (File.Exists(outFile))
            {
                outFile = outFile.Substring(0, outFile.Length - outFileSuffix.Length) + copy + outFileSuffix;
                copy++;
            }

            ProcessStartInfo fuseProcessInfo = new ProcessStartInfo(_config.Audio2TapePath)
            {
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };
            fuseProcessInfo.ArgumentList.Add(wavFile);
            fuseProcessInfo.ArgumentList.Add(outFile);
            var fuseProcess = System.Diagnostics.Process.Start(fuseProcessInfo) ?? throw new Exception("Failed to start process");
            fuseProcess.OutputDataReceived += logCallback;
            fuseProcess.ErrorDataReceived += logCallback;
            fuseProcess.BeginOutputReadLine();
            fuseProcess.BeginErrorReadLine();
            await fuseProcess.WaitForExitAsync(cancellationToken);
            if (!fuseProcess.HasExited || fuseProcess.ExitCode != 0)
            {
                // TODO: check other failure modes
                process.Status = ProcessStatus.Failed;
                process.StatusDetail = StatusDetail.FailedToConvertArtefact.ToString();
                return null;
            }

            outFiles.Add(new(outFile, DeviceID.FromQID(DeviceID.Files)));
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