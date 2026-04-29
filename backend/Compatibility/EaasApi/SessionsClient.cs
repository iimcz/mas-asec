using RestSharp;

namespace asec.Compatibility.EaasApi;

public class SessionsClient : BaseEaasClient
{
    public SessionsClient(IConfiguration configuration) : base(configuration)
    {
    }

    public async Task Keepalive(string sessionId)
    {
        RestRequest request = new($"/sessions/{sessionId}/keepalive");
        await _client.PostAsync(request);
    }
}
