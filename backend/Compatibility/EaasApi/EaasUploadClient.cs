
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
        RestRequest request = new("/upload");
        string filename = Path.GetFileName(filepath);
        request.AlwaysSingleFileAsContent = true;
        request.AddFile(Path.GetFileName(filename), filepath, ContentType.Binary);
        request.AddQueryParameter("filename", filename);
        
        return await _client.PostAsync<UploadResponse>(request, cancellationToken);
    }
}