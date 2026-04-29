using asec.Compatibility.EaasApi.Models;
using RestSharp;

namespace asec.Compatibility.EaasApi;

public class NetworksClient : BaseEaasClient
{
    public NetworksClient(IConfiguration configuration) : base(configuration)
    {
    }

    public async Task<NetworkResponse> StartNetwork(NetworkRequest inRequest)
    {
        RestRequest request = new("/networks");
        request.AddJsonBody(inRequest);
        Console.WriteLine(System.Text.Json.JsonSerializer.Serialize(inRequest));

        return await _client.PostAsync<NetworkResponse>(request);
    }
}
