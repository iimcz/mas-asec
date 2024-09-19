namespace asec.Compatibility.EaasApi.Models;

public record ObjectArchivesResponse(
    string status,
    string message,
    List<string> archives
) : EmilResponseType(status, message);