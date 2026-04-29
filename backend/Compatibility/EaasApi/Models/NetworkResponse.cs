namespace asec.Compatibility.EaasApi.Models;

[System.Diagnostics.CodeAnalysis.SuppressMessage("Naming rule violation", "IDE1006")]
public record NetworkResponse (
    string id,
    bool isLocalMode,
    Dictionary<string, string> networkUrls
);
