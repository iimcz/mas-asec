namespace asec.Compatibility.EaasApi.Models;

[System.Diagnostics.CodeAnalysis.SuppressMessage("Naming rule violation", "IDE1006")]
public record Drive(
    string id,
    object data,
    bool bootable = false
);
