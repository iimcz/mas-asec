using asec.Compatibility.EaasApi.Models;
using RestSharp;

namespace asec.Compatibility.EaasApi;

public class EmilEnvironmentDataClient : BaseEaasClient
{
    public EmilEnvironmentDataClient(IConfiguration configuration) : base(configuration)
    {
    }

    public async Task<EmilResponseType> DeleteEnvironment(EnvironmentDeleteRequest request)
    {
        RestRequest restRequest = new("/EmilEnvironmentData/delete");
        restRequest.AddJsonBody(request);

        var response = await _client.PostAsync<EmilResponseType>(restRequest);
        return response;
    }
}
