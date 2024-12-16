
using asec.Compatibility.EaasApi.Models;
using RestSharp;

namespace asec.Compatibility.EaasApi;

/// <summary>
/// EaaS client class for uploading files, so that the files can be mounted to a VM (environment/component) later.
/// </summary>
public class EaasUploadClient : BaseEaasClient
{
    public EaasUploadClient(IConfiguration configuration) : base(configuration)
    {
    }

    /// <summary>
    /// Upload the specified file to EaaS.
    /// </summary>
    /// <param name="filepath">Path to the file to upload</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>EaaS response to the upload, which includes the upload status and the list of uploaded files.</returns>
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