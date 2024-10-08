namespace asec.Compatibility.EaasApi.Models;

public record RemovableMedia(
    string id,
    string archive,
    string driveIndex
);

public record MachineComponentResponse(
    string id,
    string driveId,
    List<RemovableMedia> removableMediaList
) : ComponentResponse(id);