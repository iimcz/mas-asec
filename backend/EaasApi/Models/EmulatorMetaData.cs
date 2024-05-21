namespace asec.EaasApi.Models;

public record Provenance(
    string url,
    string tag,
    List<String> layers
);

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