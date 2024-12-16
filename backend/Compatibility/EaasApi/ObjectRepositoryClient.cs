
using asec.Compatibility.EaasApi.Models;
using RestSharp;

namespace asec.Compatibility.EaasApi;

/// <summary>
/// EaaS client for interacting with the object repository. Can be used to convert
/// uploaded files into actual objects usable for mounting into components (emulator/VM).
/// </summary>
public class ObjectRepositoryClient : BaseEaasClient
{
    private readonly string _repositoryUrl = "/object-repository/";

    public ObjectRepositoryClient(IConfiguration configuration) : base(configuration)
    {
    }

    /// <summary>
    /// Import an (already uploaded) file into EaaS as an object. This makes it usable by the rest of EaaS
    /// for mounting into VMs and emulators.
    /// </summary>
    /// <param name="request">Request containing which uploaded file (or files) to import as an object</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>EaaS ID of the resulting object</returns>
    public async Task<string> ImportObjects(ImportObjectRequest request, CancellationToken cancellationToken)
    {
        RestRequest archivesRequest = new(_repositoryUrl + "archives");
        var archives = await _client.GetAsync<ObjectArchivesResponse>(archivesRequest, cancellationToken);
        var defaultArchive = archives?.archives[0];

        RestRequest importRequest = new(_repositoryUrl + $"archives/{defaultArchive}/objects");
        importRequest.AddJsonBody(request);
        var task = await _client.PostAsync<TaskStateResponse>(importRequest, cancellationToken);

        // TODO: forward tasks back to allow polling, instead of blocking until task completion.
        while (!task!.isDone && !cancellationToken.IsCancellationRequested)
        {
            await Task.Delay(1000, cancellationToken);
            RestRequest taskRequest = new("/tasks/" + task.taskId);
            task = await _client.GetAsync<TaskStateResponse>(taskRequest, cancellationToken);
        }

        return task.userData?["objectId"];
    }
}