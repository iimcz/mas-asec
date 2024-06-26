using asec.Compatibility.EaasApi.Models;
using RestSharp;

namespace asec.Compatibility.EaasApi;
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

    public async Task<IEnumerable<EmulatorMetaData>> GetEmulators()
    {
        var request = new RestRequest("/emulator-repository/emulators");
        return await _client.GetAsync<IEnumerable<EmulatorMetaData>>(request);
    }
}