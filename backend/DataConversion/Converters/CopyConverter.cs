using asec.Models.Digitalization;
using asec.LongRunning;
using asec.Compatibility.EaasApi.Models;

namespace asec.DataConversion.Converters;

public class CopyConverterConfig : ConverterConfig
{
    public override IConverter ConstructConverter()
    {
        return new CopyConverter(this);
    }
}

public class CopyConverter : IConverter
{
    public Guid Id { get; set; }

    public string Name => "Simple copy converter";

    public string Version { get; private set; } = "";

    public string Environment {
        get {
            // TODO: maybe separate non-pure-environmental variables, like target format, elsewhere
            string OS = System.Environment.OSVersion.VersionString;
            string CLR = System.Environment.Version.ToString();
            return $"OS:{OS},CLR:{CLR}";
        }
    }

    public IList<ArtefactType> SupportedArtefactTypes => new List<ArtefactType> {
        ArtefactType.ZipArchive
    };

    private CopyConverterConfig _config;

    public CopyConverter(CopyConverterConfig config)
    {
        _config = config;
    }

    public async Task<ConversionResult> Start(Process process, CancellationToken cancellationToken)
    {
        if (process.Artefacts.Any(a => a.Type != ArtefactType.ZipArchive))
        {
            process.Status = ProcessStatus.Failed;
            process.StatusDetail = "InvalidInput";
            return null;
        }
        using var logStream = new FileStream(process.LogPath, FileMode.Append);
        using var logWriter = new StreamWriter(logStream);
        logWriter.AutoFlush = true;

        process.Status = ProcessStatus.Running;
        List<ConvertedFile> outFiles = new();
        foreach (Artefact a in process.Artefacts)
        {
            logWriter.WriteLine($"Copying artefact: {a.Id} (note: {a.InternalNote})");
            string zipFile = await process.FetchArtefact(a, false, cancellationToken);
            outFiles.Add(new(zipFile, DeviceID.FromQID(DeviceID.Files)));
        }

        logWriter.WriteLine("Conversion process finished successfully.");

        process.Status = ProcessStatus.Success;
        return new(outFiles);
    }
}

