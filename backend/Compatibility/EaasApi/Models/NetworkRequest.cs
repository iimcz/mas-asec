namespace asec.Compatibility.EaasApi.Models;

[System.Diagnostics.CodeAnalysis.SuppressMessage("Naming rule violation", "IDE1006")]
public record ComponentSpec (
    string componentId,
    string networkLabel,
    List<short> serverPorts,
    string serverIp,
    string fqdn,
    string hwAddress = "auto",
    bool ephemeral = true
);

[System.Diagnostics.CodeAnalysis.SuppressMessage("Naming rule violation", "IDE1006")]
public record TcpGatewayConfig (
    bool socks,
    string serverPort,
    string serverIp
);

[System.Diagnostics.CodeAnalysis.SuppressMessage("Naming rule violation", "IDE1006")]
public record NetworkRequest (
    List<ComponentSpec> components,
    bool hasInternet,
    bool enableDhcp,
    bool hasTcpGateway,
    string lifetime,
    string gateway,
    string network,
    TcpGatewayConfig tcpGatewayConfig
);
