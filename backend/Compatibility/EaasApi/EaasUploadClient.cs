
using asec.Compatibility.EaasApi.Models;
using RestSharp;

namespace asec.Compatibility.EaasApi;

public class EaasUploadClient : BaseEaasClient
{
    public EaasUploadClient(IConfiguration configuration) : base(configuration)
    {
    }

    public async Task<UploadResponse> Upload(string filepath, CancellationToken cancellationToken)
    {
        RestRequest request = new();
        string filename = Path.GetFileName(filepath);
        request.AddFile(Path.GetFileName(filename), filepath);
        request.AddQueryParameter("filename", filename);
        
        return await _client.PostAsync<UploadResponse>(request, cancellationToken);
    }
}