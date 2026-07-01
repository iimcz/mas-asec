namespace asec.Compatibility.EaasApi.Models;

[System.Diagnostics.CodeAnalysis.SuppressMessage("Naming rule violation", "IDE1006")]
public record EnvironmentDeleteRequest(
    bool deleteImage,
    bool deleteMetaData,
    string envId,
    bool force
);
