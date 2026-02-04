using System.Text.Json;
using System.Text.Json.Nodes;

namespace asec.Compatibility.CollectiveAccess;

public class CollectiveAccessAuth
{
    private const string AUTH_QUERY = "query login($uname:String,$pwd:String){login(username:$uname,password:$pwd){jwt}}";
    private const string CONFIG_SECTION = "CollectiveAccessAPI";

    private readonly SemaphoreSlim _semaphore = new(1, 1);
    
    private readonly IHttpClientFactory _clientFactory;
    private readonly JsonSerializerOptions _jsonOptions;
    private readonly string _baseUrl;
    private readonly string _username;
    private readonly string _password;
    private readonly TimeSpan _tokenValidity;
    
    private string _token;
    private DateTime _tokenTimeStamp;

    public CollectiveAccessAuth(IConfiguration configuration, IHttpClientFactory clientFactory)
    {
        _clientFactory = clientFactory;

        _jsonOptions = new() {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        var section = configuration.GetSection(CONFIG_SECTION);
        _baseUrl = section.GetValue<string>("BaseUrl");
        _username = section.GetValue<string>("Username");
        _password = section.GetValue<string>("Password");
        _tokenValidity = section.GetValue<TimeSpan>("TokenValidity");
    }

    public async Task<string> GetValidTokenAsync(CancellationToken cancellationToken = default(CancellationToken))
    {
        // Use a semaphore so that we don't try to refresh a token multiple times in parallel.
        await _semaphore.WaitAsync();

        if (_tokenTimeStamp + _tokenValidity < DateTime.Now)
        {
            _semaphore.Release();
            return _token;
        }

        try
        {
            var client = _clientFactory.CreateClient();
            client.BaseAddress = new(_baseUrl);

            var request = new GraphQLRequest<AuthParams>() {
                Query = AUTH_QUERY,
                Variables = new() {
                    Uname = _username,
                    Pwd = _password
                }
            };

            var response = await client.PostAsJsonAsync("service.php/auth", request, _jsonOptions, cancellationToken);

            // TODO: proper handling / logging
            response.EnsureSuccessStatusCode();

            var data = await response.Content.ReadFromJsonAsync<GraphQLResponse<AuthData>>(_jsonOptions, cancellationToken);
            if (data == null || !data.Ok)
            {
                // TODO: also proper handling
                throw new ApplicationException(data?.Errors?.ToString());
            }

            _token = data.Data.Jwt;
            _tokenTimeStamp = DateTime.Now;
            return _token;
        }
        finally
        {
                _semaphore.Release();
        }
    }

    public class AuthParams
    {
        public string Uname {get; set;}
        public string Pwd {get; set;}
    }

    public class AuthData
    {
        public JsonObject Login {get; set;}

        public string Jwt {
            get {
                return Login["jwt"]?.ToString() ?? "";
            }
        }
    }
}
