namespace asec.Compatibility.EaasApi.Models;

[System.Diagnostics.CodeAnalysis.SuppressMessage("Naming rule violation", "IDE1006")]
public record Provenance(
    string url,
    string tag,
    List<String> layers
);

[System.Diagnostics.CodeAnalysis.SuppressMessage("Naming rule violation", "IDE1006")]
public record EmulatorMetaData(
    string kind,
    string id,
    string name,
    string version,
    string digest,
    ISet<String> tags,
    Provenance provenance,
    ImageMetaData image
);