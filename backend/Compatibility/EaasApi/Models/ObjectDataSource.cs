namespace asec.Compatibility.EaasApi.Models;

[System.Diagnostics.CodeAnalysis.SuppressMessage("Naming rule violation", "IDE1006")]
public record ObjectDataSource(
    string id,
    string archive = "default"
) : DriveDataSource("object");