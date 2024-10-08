namespace asec.Compatibility.EaasApi.Models;

public record Drive(
    string id,
    ObjectDataSource data,
    bool bootable = false
);