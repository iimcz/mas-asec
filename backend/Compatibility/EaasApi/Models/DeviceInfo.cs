namespace asec.Compatibility.EaasApi.Models;

[System.Diagnostics.CodeAnalysis.SuppressMessage("Naming rule violation", "IDE1006")]
public record DeviceInfo(
    string vendor,
	string device,
	int idVendor,
	int idDevice,
	string connectCommand,
	string disconnectCommand,
	string deviceType
);