using System.Text.Json.Serialization;

namespace asec.Compatibility.EaasApi.Models;

public record TaskStateResponse(
    string status,
    string message,
    string taskId,
    bool isDone,
    Dictionary<string, string> userData,
    [property: JsonPropertyName("object")] string object_
) : EmilResponseType(status, message);