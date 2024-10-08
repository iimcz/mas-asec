namespace asec.Compatibility.EaasApi.Models;

public record ObjectDataSource(
    string id,
    string archive = "default"
) : DriveDataSource("object");