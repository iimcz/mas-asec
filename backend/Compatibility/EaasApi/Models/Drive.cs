namespace asec.Compatibility.EaasApi.Models;

[System.Diagnostics.CodeAnalysis.SuppressMessage("Naming rule violation", "IDE1006")]
public record Drive(
    string id,
    ObjectDataSource data,
    bool bootable = false
);