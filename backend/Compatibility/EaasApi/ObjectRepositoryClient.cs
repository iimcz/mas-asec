
using asec.Compatibility.EaasApi.Models;
using RestSharp;

namespace asec.Compatibility.EaasApi;

public class ObjectRepositoryClient : BaseEaasClient
{
    private readonly string _repositoryUrl = "/object-repository/";

    public ObjectRepositoryClient(IConfiguration configuration) : base(configuration)
    {
    }

    public async Task ImportObjects(ImportObjectRequest request, CancellationToken cancellationToken)
    {
        RestRequest archivesRequest = new(_repositoryUrl + "archives");
        var archives = await _client.GetAsync<ObjectArchivesResponse>(archivesRequest, cancellationToken);
        var defaultArchive = archives?.archives[0];

        RestRequest importRequest = new(_repositoryUrl + defaultArchive + "/objects");
        importRequest.AddJsonBody(request);
        var task = await _client.PostAsync<TaskStateResponse>(importRequest);

        // TODO: forward tasks back to allow polling, instead of blocking until task completion.
        while (!task.isDone && !cancellationToken.IsCancellationRequested)
        {
            await Task.Delay(1000);
            RestRequest taskRequest = new("/tasks/" + task.taskId);
            task = await _client.GetAsync<TaskStateResponse>(taskRequest);
        }
    }
}