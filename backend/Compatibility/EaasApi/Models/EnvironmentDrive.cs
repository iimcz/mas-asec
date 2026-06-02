namespace asec.Compatibility.EaasApi.Models;

[System.Diagnostics.CodeAnalysis.SuppressMessage("Naming rule violation", "IDE1006")]
public record EnvironmentDrive(
      string data,
      string iface,
      string bus,
      string unit,
      string type,
      string filesystem,
      bool boot,
      bool plugged
);
