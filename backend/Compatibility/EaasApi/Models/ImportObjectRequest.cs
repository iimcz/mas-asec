namespace asec.Compatibility.EaasApi.Models;

public record ImportFileInfo(
    string url,
    string deviceId,
    string fileFmt,
    string filename
);

public record ImportObjectRequest(
    string label,
    List<ImportFileInfo> files
);