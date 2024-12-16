namespace asec.Compatibility.EaasApi.Models;

[System.Diagnostics.CodeAnalysis.SuppressMessage("Naming rule violation", "IDE1006")]
public record ComponentStateResponse(
    string id,
    string state
) : ComponentResponse(id);