using asec.Compatibility.EaasApi.Models;
using RestSharp;
using ControlUrlMap = System.Collections.Generic.Dictionary<System.String, System.Uri>;

namespace asec.Compatibility.EaasApi;

public class ComponentsClient : BaseEaasClient
{

    public ComponentsClient(IConfiguration configuration) : base(configuration)
    {
    }

    public async Task<ComponentResponse> StartComponent(ComponentRequest request)
    {
        if (request is MachineComponentRequest machineRequest)
            return await StartMachineComponent(machineRequest);
        throw new NotImplementedException("Component request types other than MachineComponentRequest are not supported.");
    }

    public async Task<ComponentResponse> GetComponent(string componentId)
    {
        RestRequest request = new($"/components/{componentId}");

        // TODO: this just ugly checks that fields that are unique to MachineComponentResponse
        // are present and if not, returns a new ComponentResponse
        var response = await _client.GetAsync<MachineComponentResponse>(request);
        if (response.driveId == null && response.removableMediaList == null)
            return new(response.id);
        return response;
    }

    public async Task Keepalive(string componentId)
    {
        RestRequest request = new($"/components/{componentId}/keepalive");
        await _client.PostAsync(request);
    }

    public async Task<ComponentStateResponse> GetComponentState(string componentId)
    {
        RestRequest request = new($"/components/{componentId}/state");
        return await _client.GetAsync<ComponentStateResponse>(request);
    }

    public async Task<ControlUrlMap> GetControlUrls(string componentId)
    {
        RestRequest request = new($"/components/{componentId}/controlurls");
        return await _client.GetAsync<ControlUrlMap>(request);
    }

    public async Task StopComponent(string componentId)
    {
        throw new NotImplementedException();
        //RestRequest request = new($"/components/{componentId}/stop");
        //request.Interceptors.Add()
        //await _client.GetAsync<ProcessResultUrl>
    }

    private async Task<MachineComponentResponse> StartMachineComponent(MachineComponentRequest inRequest)
    {
        RestRequest request = new("/components");
        request.AddJsonBody(inRequest);
        return await _client.PostAsync<MachineComponentResponse>(request);
    }
}