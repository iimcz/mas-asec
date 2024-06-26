using asec.ViewModels;
using RestSharp;

namespace asec.Compatibility.RemoteLLMApi;
public class RemoteLLMClient
{
    private const string CONFIG_SECTION = "RemoteLLM";
    private RestClient _client;

    public RemoteLLMClient(IConfiguration config)
    {
        var section = config.GetSection(CONFIG_SECTION);
        var baseUrl = section.GetValue<string>("BaseURL");
        if (string.IsNullOrEmpty(baseUrl))
        {
            throw new ArgumentException("Missing baseUrl for Remote LLM API");
        }
        _client = new RestClient(baseUrl);
    }

    public async Task<Work> TryDeduceWorkPropertiesFrom(string sourceUrl)
    {
        var request = new RestRequest("/emulator-repository/emulators", Method.Post);
        request.AddBody(sourceUrl);
        return await _client.PostAsync<Work>(request);
    }
}