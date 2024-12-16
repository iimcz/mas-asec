using asec.Compatibility.EaasApi.Models;
using RestSharp;

namespace asec.Compatibility.EaasApi.ControlUrls;

/// <summary>
/// Client class wrapping commands available on the QemuControlUrl EaaS connector.
/// Allows requesting USB devices connected to the EaaS host available for passthrough and
/// requesting that they be passed through to the guest VM.
/// </summary>
public class QemuControlUrlClient
{
    private RestClient _client;

    /// <summary>
    /// Initialize the REST client for the QemuControlUrl connector.
    /// </summary>
    /// <param name="url">URL of the connector, usually supplied by EaaS when the emulation components are created</param>
    public QemuControlUrlClient(Uri url)
    {
        _client = new RestClient(url);
    }

    /// <summary>
    /// Request information about devices available for passthrough - this includes the appropriate commands to both
    /// enable and disable passthrough for each device.
    /// </summary>
    /// <param name="cancellationToken">Async cancellation token</param>
    /// <returns>List of devices available for passthrough</returns>
    public async Task<List<DeviceInfo>> GetDeviceInfos(CancellationToken cancellationToken = default)
    {
        var request = new RestRequest("");
        return await _client.GetAsync<List<DeviceInfo>>(request, cancellationToken);
    }

    /// <summary>
    /// Send the specified command to the underlying QEMU process running the VM. This will usually be a command
    /// supplied in the device list from <see cref="GetDeviceInfos(CancellationToken)"/>.
    /// </summary>
    /// <param name="cmd">Command to pass to QEMU</param>
    /// <param name="cancellationToken">Async cancellation token</param>
    /// <returns>Nothing</returns>
    public async Task PostCommand(string cmd, CancellationToken cancellationToken = default)
    {
        var request = new RestRequest("");
        request.AddStringBody(cmd, DataFormat.None);
        await _client.PostAsync(request, cancellationToken);
    }
}