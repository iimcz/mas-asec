namespace asec.EaasApi.Models;

public record ImageMetaData(
    string kind,
    string id,
	string fstype,
	string category,
	string label
);