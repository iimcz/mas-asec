namespace asec.Compatibility.EaasApi.Models;

[System.Diagnostics.CodeAnalysis.SuppressMessage("Naming rule violation", "IDE1006")]
public record ImageMetaData(
    string kind,
    string id,
	string fstype,
	string category,
	string label
);