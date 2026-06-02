using asec.Compatibility.EaasApi.Models;
using RestSharp;

namespace asec.Compatibility.EaasApi;

/// <summary>
/// EaaS client for interacting with the environment repository. Can be used to import
/// uploaded files as disk images, so that they can later be mounted into environments.
/// </summary>
public class EnvironmentRepositoryClient : BaseEaasClient
{
    private readonly string _repositoryUrl = "/environment-repository/";

    public EnvironmentRepositoryClient(IConfiguration configuration) : base(configuration)
    {
    }

    /// <summary>
    /// Import an (already uploaded) disk image into EaaS. This makes it usable by the rest
    /// of EaaS for mounting into VMs and emulators.
    /// </summary>
    /// <param name="request">Request specifying which uploaded file to use for image import</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>EaaS ID of the imported image</returns>
    public async Task<string> ImportImage(ImportImageRequest request, CancellationToken cancellationToken = default(CancellationToken))
    {
        RestRequest importRequest = new(_repositoryUrl + "actions/import-image");
        importRequest.AddJsonBody(request);
        var task = await _client.PostAsync<TaskStateResponse>(importRequest, cancellationToken);

        // TODO: forward tasks back to allow polling, instead of blocking until task completion.
        while (!task!.isDone && !cancellationToken.IsCancellationRequested)
        {
            await Task.Delay(1000, cancellationToken);
            RestRequest taskRequest = new("/tasks/" + task.taskId);
            task = await _client.GetAsync<TaskStateResponse>(taskRequest, cancellationToken);
        }

        return task.userData?["imageId"];
    }

    public async Task<EnvironmentDetails> GetEnvironmentDetails(string environmentId, CancellationToken cancellationToken = default)
    {
        RestRequest detailsRequest = new(_repositoryUrl + $"environments/{environmentId}");
        var details = await _client.GetAsync<EnvironmentDetails>(detailsRequest, cancellationToken);
        return details;
    }

    public async Task<string> DownloadImage(string imageId, string outputFolder, CancellationToken cancellationToken = default)
    {
        RestRequest downloadRequest = new(_repositoryUrl + $"images/{imageId}/url");
        var stream = await _client.DownloadStreamAsync(downloadRequest, cancellationToken);

        if (stream == null)
            return null;

        var outputFile = Path.Combine(outputFolder, imageId);

        using (FileStream outputStream = new FileStream(outputFile, FileMode.CreateNew))
        {
            await stream.CopyToAsync(outputStream);
        }

        return outputFile;
    }
}
