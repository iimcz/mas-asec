using asec.Compatibility.EaasApi.Models;
using RestSharp;
using ControlUrlMap = System.Collections.Generic.Dictionary<System.String, System.Uri>;

namespace asec.Compatibility.EaasApi;

/// <summary>
/// EaaS service client for managing components (storage, emulators/VMs, etc.)
/// Allows starting, stopping components, managing their lifecycle and getting various information about them.
/// </summary>
public class ComponentsClient : BaseEaasClient
{
    public ComponentsClient(IConfiguration configuration) : base(configuration)
    {
    }

    /// <summary>
    /// Sends a request to EaaS to start components as specified in the supplied request.
    /// </summary>
    /// <param name="request">EaaS component start request</param>
    /// <returns>EaaS response</returns>
    /// <exception cref="NotImplementedException">Thrown when a component type other than <see cref="MachineComponentRequest"/> is requested.</exception>
    public async Task<ComponentResponse> StartComponent(ComponentRequest request)
    {
        if (request is MachineComponentRequest machineRequest)
            return await StartMachineComponent(machineRequest);
        throw new NotImplementedException("Component request types other than MachineComponentRequest are not supported.");
    }

    /// <summary>
    /// Gets information about the specified component from EaaS.
    /// </summary>
    /// <param name="componentId">EaaS ID of the component to get information for</param>
    /// <returns>Either <see cref="MachineComponentResponse"/> or <see cref="ComponentResponse"/> as appropriate</returns>
    public async Task<ComponentResponse> GetComponent(string componentId)
    {
        RestRequest request = new($"/components/{componentId}");

        // TODO: this just ugly checks that fields that are unique to MachineComponentResponse
        // are present and if not, returns a new ComponentResponse
        var response = await _client.GetAsync<MachineComponentResponse>(request);
        // TODO: include better checks for success (and appropriate return values)
        if (response == null)
            return null;
        if (response.driveId == null && response.removableMediaList == null)
            return new(response.id);
        return response;
    }

    /// <summary>
    /// Sends a keepalive request to EaaS for the specified component. This is to ensure EaaS doesn't
    /// terminate the component prematurely.
    /// </summary>
    /// <param name="componentId">EaaS ID of the component</param>
    /// <returns>Nothing</returns>
    public async Task Keepalive(string componentId)
    {
        RestRequest request = new($"/components/{componentId}/keepalive");
        await _client.PostAsync(request);
    }

    /// <summary>
    /// Gets the current EaaS state of the requested component.
    /// </summary>
    /// <param name="componentId">EaaS ID of the component</param>
    /// <returns>State of the requested component</returns>
    public async Task<ComponentStateResponse> GetComponentState(string componentId)
    {
        RestRequest request = new($"/components/{componentId}/state");
        return await _client.GetAsync<ComponentStateResponse>(request);
    }

    /// <summary>
    /// Get EaaS control URLs for the specified component. These can be used depending on the
    /// URL type, like sending commands to qemu, setting up network connections, etc.
    /// </summary>
    /// <param name="componentId">EaaS ID of the component</param>
    /// <returns>Map of the control URLs (maps string of the type to an URL)</returns>
    public async Task<ControlUrlMap> GetControlUrls(string componentId)
    {
        RestRequest request = new($"/components/{componentId}/controlurls");
        return await _client.GetAsync<ControlUrlMap>(request);
    }

    /// <summary>
    /// Request that EaaS stops the specified component. Usually means stopping the emulator or VM.
    /// </summary>
    /// <param name="componentId">EaaS ID of the component</param>
    /// <returns>Nothing</returns>
    public async Task StopComponent(string componentId)
    {
        RestRequest request = new($"/components/{componentId}/stop");
        await _client.GetAsync<ProcessResultUrl>(request);

        // TODO: save emulator log from the above returned url, if not null.
    }

    /// <summary>
    /// Start an EaaS component of the machine type. This usually means starting an environment - a VM
    /// for running an emulator or other applications.
    /// </summary>
    /// <param name="inRequest">EaaS request containing which environment to start</param>
    /// <returns>EaaS response containing the component ID and other information about the started component</returns>
    private async Task<MachineComponentResponse> StartMachineComponent(MachineComponentRequest inRequest)
    {
        RestRequest request = new("/components");
        request.AddJsonBody(inRequest);
        return await _client.PostAsync<MachineComponentResponse>(request);
    }
}