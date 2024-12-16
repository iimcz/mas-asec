namespace asec.Compatibility.EaasApi.Models;

[System.Diagnostics.CodeAnalysis.SuppressMessage("Naming rule violation", "IDE1006")]
public record UploadedItem(
    string url,
    string filename
);

[System.Diagnostics.CodeAnalysis.SuppressMessage("Naming rule violation", "IDE1006")]
public record UploadResponse(
    string status,
    string message,
    List<string> uploads, /* deprecated */
    List<UploadedItem> uploadedItemList
) : EmilResponseType(status, message);