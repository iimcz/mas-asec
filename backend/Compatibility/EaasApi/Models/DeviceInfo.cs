namespace asec.Compatibility.EaasApi.Models;

public record DeviceInfo(
    string vendor,
	string device,
	int idVendor,
	int idDevice,
	string connectCommand,
	string disconnectCommand,
	string deviceType
);