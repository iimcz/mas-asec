namespace asec.Compatibility.EaasApi.Models;

public record SnapshotRequest(
    string type,
    string message,
    bool isRelativeMouse,
    bool cleanRemovableDrives,
    string envId
);
