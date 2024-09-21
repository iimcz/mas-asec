namespace asec.Compatibility.EaasApi.Models;

public record UploadedItem(
    string url,
    string filename
);

public record UploadResponse(
    string status,
    string message,
    List<string> uploads, /* deprecated */
    List<UploadedItem> uploadedItemList
) : EmilResponseType(status, message);