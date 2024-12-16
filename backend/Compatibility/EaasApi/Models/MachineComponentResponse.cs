namespace asec.Compatibility.EaasApi.Models;

[System.Diagnostics.CodeAnalysis.SuppressMessage("Naming rule violation", "IDE1006")]
public record RemovableMedia(
    string id,
    string archive,
    string driveIndex
);

[System.Diagnostics.CodeAnalysis.SuppressMessage("Naming rule violation", "IDE1006")]
public record MachineComponentResponse(
    string id,
    string driveId,
    List<RemovableMedia> removableMediaList
) : ComponentResponse(id);