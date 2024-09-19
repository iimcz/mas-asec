using RestSharp;

namespace asec.Compatibility.EaasApi;

public abstract class BaseEaasClient
{

    private const string CONFIG_SECTION = "EaaSAPI";
    protected RestClient _client;

    public BaseEaasClient(IConfiguration configuration)
    {
        var section = configuration.GetSection(CONFIG_SECTION);
        var baseUrl = section.GetValue<string>("BaseURL");
        if (string.IsNullOrEmpty(baseUrl))
        {
            throw new ArgumentException("Missing baseUrl for EaaS API");
        }
        _client = new RestClient(baseUrl);
    }
}