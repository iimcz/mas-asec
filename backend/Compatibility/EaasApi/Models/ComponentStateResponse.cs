namespace asec.Compatibility.EaasApi.Models;

public record ComponentStateResponse(
    string id,
    string state
) : ComponentResponse(id);