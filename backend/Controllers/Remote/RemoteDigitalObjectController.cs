using asec.Compatibility.CollectiveAccess;
using asec.Compatibility.CollectiveAccess.Models;
using asec.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace asec.Controllers.Remote;

[ApiController]
[Route("/api/v1/remotedigitalobjects")]
public class RemoteDigitalObjectController : ControllerBase
{
    private readonly SearchClient _searchClient;
    private readonly EditClient _editClient;

    public RemoteDigitalObjectController(SearchClient searchClient, EditClient editClient)
    {
        _searchClient = searchClient;
        _editClient = editClient;
    }

    [HttpGet("")]
    [Produces(typeof(List<ViewModels.RemoteDigitalObject>))]
    public async Task<IActionResult> ListRemoteDigitalObjects(string searchTerm)
    {
        var caDigiObjects = await _searchClient.GetDigitalObjects(searchTerm);
        // TODO: exception handling / handle failed GetDigitalObjects
        return Ok(caDigiObjects.Select(d => new RemoteDigitalObject() {
            Id = d.Id,
            Idno = d.Idno,
            Label = d.Bundles.GetOptionalBundleValue(BundleCodes.ObjectLabel),
            Note = d.Bundles.GetOptionalBundleValue(BundleCodes.ObjectInternalNote)
        }));
    }

    [HttpGet("{id:int}")]
    [Produces(typeof(ViewModels.RemoteDigitalObject))]
    public async Task<IActionResult> GetRemoteDigitalObjects(int id)
    {
        throw new NotImplementedException();
    }

    [HttpPost("{digitalObjectId:int}/link")]
    public async Task<IActionResult> LinkRemoteDigitalObject(int digitalObjectId, [FromBody] LinkRemoteDigitalObjectCommand linkCommand)
    {
        var result = await _editClient.LinkObjectManifestationToOccurrance(digitalObjectId, linkCommand.RemoteParatextId);
        if (result) return Ok();

        // TODO: better error handling?
        throw new ApplicationException("Failed to link object!");
    }

    [HttpPost("{digitalObjectId:int}/unlink")]
    public async Task<IActionResult> UnlinkRemoteDigitalObject(int digitalObjectId, [FromBody] LinkRemoteDigitalObjectCommand linkCommand)
    {
        var result = await _editClient.UnlinkObjectManifestationToOccurrence(digitalObjectId, linkCommand.RemoteParatextId);
        if (result) return Ok();

        // TODO: better error handling?
        throw new ApplicationException("Failed to unlink object!");
    }

    public sealed record LinkRemoteDigitalObjectCommand(int RemoteParatextId);
}
