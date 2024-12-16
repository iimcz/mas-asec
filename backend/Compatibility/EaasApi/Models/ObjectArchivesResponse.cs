namespace asec.Compatibility.EaasApi.Models;

[System.Diagnostics.CodeAnalysis.SuppressMessage("Naming rule violation", "IDE1006")]
public record ObjectArchivesResponse(
    string status,
    string message,
    List<string> archives
) : EmilResponseType(status, message);