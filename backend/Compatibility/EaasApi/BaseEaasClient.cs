using RestSharp;

namespace asec.Compatibility.EaasApi;

/// <summary>
/// Basic RestClient wrapper for EaaS services clients. Takes the EaaS base URL from the supplied
/// configuration and allows child classes to just use the configured _client field for requests.
/// </summary>
public abstract class BaseEaasClient
{
    private const string CONFIG_SECTION = "EaaSAPI";

    /// <summary>
    /// Derived classes should use this field for requests, as it will already be configured with
    /// the correct base URL from application configuration.
    /// </summary>
    protected RestClient _client;

    /// <summary>
    /// Constructs the base client, setting up the RestClient field.
    /// </summary>
    /// <param name="configuration">App configuration from which to gather base URL of EaaS services</param>
    /// <exception cref="ArgumentException">Thrown when the BaseURL key cannot be found</exception>
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