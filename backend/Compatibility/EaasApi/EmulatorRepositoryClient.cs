using asec.Compatibility.EaasApi.Models;
using RestSharp;

namespace asec.Compatibility.EaasApi;

/// <summary>
/// EaaS client for working with EaaS emulators (currently different to emulators mentioned elsewhere in this repo).
/// </summary>
public class EmulatorRepositoryClient
{
    private const string CONFIG_SECTION = "EaaSAPI";
    private RestClient _client;

    public EmulatorRepositoryClient(IConfiguration config)
    {
        var section = config.GetSection(CONFIG_SECTION);
        var baseUrl = section.GetValue<string>("BaseURL");
        if (string.IsNullOrEmpty(baseUrl))
        {
            throw new ArgumentException("Missing baseUrl for EaaS API");
        }
        _client = new RestClient(baseUrl);
    }

    /// <summary>
    /// Request the list of available emulators from EaaS.
    /// </summary>
    /// <returns>Enumetable list of available EaaS emulators</returns>
    public async Task<IEnumerable<EmulatorMetaData>> GetEmulators()
    {
        var request = new RestRequest("/emulator-repository/emulators");
        return await _client.GetAsync<IEnumerable<EmulatorMetaData>>(request);
    }
}