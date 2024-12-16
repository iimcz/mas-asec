namespace asec.Compatibility.EaasApi.Models;

[System.Diagnostics.CodeAnalysis.SuppressMessage("Naming rule violation", "IDE1006")]
public record ImportFileInfo(
    string url,
    string deviceId,
    string fileFmt,
    string filename
);

[System.Diagnostics.CodeAnalysis.SuppressMessage("Naming rule violation", "IDE1006")]
public record ImportObjectRequest(
    string label,
    List<ImportFileInfo> files
);