using System.Text.Json;

namespace asec.Compatibility.CollectiveAccess;

public abstract class BaseCollectiveAccessClient
{
    private const string CONFIG_SECTION = "CollectiveAccessAPI";

    private readonly IHttpClientFactory _clientFactory;
    private readonly JsonSerializerOptions _serializerOptions;
    private readonly CollectiveAccessAuth _auth;
    private readonly string _baseUrl;

    public BaseCollectiveAccessClient(IConfiguration configuration, IHttpClientFactory clientFactory, CollectiveAccessAuth auth)
    {
        _clientFactory = clientFactory;

        _serializerOptions = new JsonSerializerOptions() {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        _auth = auth;

        var section = configuration.GetSection(CONFIG_SECTION);
        _baseUrl = section.GetValue<string>("BaseUrl");
    }

    protected async Task<GraphQLResponse<TOutData>> PostAuthenticatedAsync<TInVars, TOutData>(string endpoint, GraphQLRequest<TInVars> request, CancellationToken cancellationToken = default(CancellationToken))  where TInVars : GraphQLAuthVars
    {
        var client = _clientFactory.CreateClient();
        client.BaseAddress = new(_baseUrl);

        request.Variables.Jwt = await _auth.GetValidTokenAsync(cancellationToken);

        var response = await client.PostAsJsonAsync(endpoint, request, _serializerOptions, cancellationToken);
        var result = await response.Content.ReadFromJsonAsync<GraphQLResponse<TOutData>>();
        // TODO: proper error handling / logging
        if (!response.IsSuccessStatusCode || (result != null && !result.Ok))
        {
            throw new HttpRequestException(result?.Errors?.ToString(), null, response.StatusCode);
        }

        return result;
    }
}
