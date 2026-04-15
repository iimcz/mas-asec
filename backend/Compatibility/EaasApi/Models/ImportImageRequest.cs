namespace asec.Compatibility.EaasApi.Models;

[System.Diagnostics.CodeAnalysis.SuppressMessage("Naming rule violation", "IDE1006")]
public record ImportImageRequest(
    string url,
    string label,
    string imageType = "USER"
);
