using asec.Compatibility.EaasApi.Models;
using RestSharp;

namespace asec.Compatibility.EaasApi.ControlUrls;

public class QemuControlUrlClient
{
    private RestClient _client;

    public QemuControlUrlClient(Uri url)
    {
        _client = new RestClient(url);
    }

    public async Task<List<DeviceInfo>> GetDeviceInfos(CancellationToken cancellationToken = default)
    {
        var request = new RestRequest("");
        return await _client.GetAsync<List<DeviceInfo>>(request, cancellationToken);
    }

    public async Task PostCommand(string cmd, CancellationToken cancellationToken = default)
    {
        var request = new RestRequest("");
        request.AddStringBody(cmd, DataFormat.None);
        await _client.PostAsync(request, cancellationToken);
    }
}