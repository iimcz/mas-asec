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
    private readonly ItemClient _itemClient;

    public RemoteDigitalObjectController(SearchClient searchClient, EditClient editClient, ItemClient itemClient)
    {
        _searchClient = searchClient;
        _editClient = editClient;
        _itemClient = itemClient;
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
        var digitalObject = await _itemClient.GetDigitalObject(id);
        return Ok(new RemoteDigitalObject() {
            Id = digitalObject.Id,
            Idno = digitalObject.Idno,
            Label = digitalObject.Bundles.GetOptionalBundleValue(BundleCodes.ObjectLabel),
            Note = digitalObject.Bundles.GetOptionalBundleValue(BundleCodes.ObjectInternalNote)
        });
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
