using asec.ViewModels;
using asec.Compatibility.CollectiveAccess;
using asec.Compatibility.CollectiveAccess.Models;
using Microsoft.AspNetCore.Mvc;

namespace asec.Controllers;

[ApiController]
[Route("/api/v1/import")]
public class ImportController : ControllerBase
{
    private readonly SearchClient _searchClient;
    private readonly ItemClient _itemClient;

    public ImportController(SearchClient searchClient, ItemClient itemClient)
    {
        _searchClient = searchClient;
        _itemClient = itemClient;
    }

    [HttpGet("available")]
    public async Task<IActionResult> GetAvailableWorks([FromQuery] string q = null, CancellationToken cancellationToken = default(CancellationToken))
    {
        var works = await _searchClient.GetWorks(q, cancellationToken);

        var result = new List<ImportableWork>();
        foreach (var work in works)
        {
            result.Add(new() {
                Id = work.Id,
                Idno = work.Idno,
                Label = work.Bundles.Where(b => b.Code == BundleCodes.OccurrenceLabel).FirstOrDefault()?.Values[0].Value,
                NumVersions = await _itemClient.GetVersionCountForWork(work, cancellationToken),
                IsAlreadyImported = false // TODO: implement check
            });
        }

        return Ok(result);
    }

    [HttpPost]
    public async Task<IActionResult> ImportWorkWithVersions([FromBody] ImportableWork work)
    {
        // TODO: implement
        return Ok();
    }
}
