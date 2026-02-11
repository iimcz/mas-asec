using asec.ViewModels;
using asec.Compatibility.CollectiveAccess;
using asec.Compatibility.CollectiveAccess.Models;
using asec.Models;
using Microsoft.AspNetCore.Mvc;

namespace asec.Controllers;

[ApiController]
[Route("/api/v1/import")]
public class ImportController : ControllerBase
{
    private readonly ILogger<ImportController> _logger;
    private readonly SearchClient _searchClient;
    private readonly ItemClient _itemClient;
    private readonly AsecDBContext _dbContext;

    public ImportController(SearchClient searchClient, ItemClient itemClient, AsecDBContext dbContext, ILogger<ImportController> logger)
    {
        _searchClient = searchClient;
        _itemClient = itemClient;
        _dbContext = dbContext;
        _logger = logger;
    }

    [HttpGet("available")]
    public async Task<IActionResult> GetAvailableWorks([FromQuery] string q = null, CancellationToken cancellationToken = default(CancellationToken))
    {
        _logger.LogInformation("Querying CA for available works.");
        var works = await _searchClient.GetWorks(q, cancellationToken);

        var imported = _dbContext.Works.Select(w => w.RemoteId);

        _logger.LogInformation("Have {0} works, querying their versions.", works.Count);
        var result = new List<ImportableWork>();
        foreach (var work in works)
        {
            result.Add(new() {
                Id = work.Id,
                Idno = work.Idno,
                Label = work.Bundles.Where(b => b.Code == BundleCodes.OccurrenceLabel).FirstOrDefault()?.Values[0].Value,
                NumVersions = await _itemClient.GetVersionCountForWork(work, cancellationToken),
                IsAlreadyImported = imported.Contains(work.Id)
            });
        }

        return Ok(result);
    }

    [HttpPost]
    public async Task<IActionResult> ImportWorkWithVersions([FromBody] ImportableWork iwork, CancellationToken cancellationToken = default(CancellationToken))
    {
        var alreadyImported = _dbContext.Works.FirstOrDefault(w => w.RemoteId == iwork.Id);
        if (alreadyImported != null)
        {
            // Refuse to import what we already have - it should be updated instead.
            return BadRequest();
        }

        _logger.LogInformation("Importing new work, ID: {0}", iwork.Id);

        var work = await _itemClient.GetWork(iwork.Id);
        var versions = await _itemClient.GetVersionsForWork(work);

        _logger.LogInformation("Found work including {0} versions.", versions.Count);

        var dbVersions = new List<Models.Archive.WorkVersion>();
        dbVersions.AddRange(
        versions.Select(wv => new Models.Archive.WorkVersion() {
                RemoteId = wv.Id,
                Label = GetOptionalBundleValue(wv.Bundles, BundleCodes.OccurrenceLabel)
            })
        );

        var dbWork = new Models.Archive.Work() {
            RemoteId = work.Id,
            Label = GetOptionalBundleValue(work.Bundles, BundleCodes.OccurrenceLabel),
            InternalNote = GetOptionalBundleValue(work.Bundles, BundleCodes.OccurrenceInternalNote),
            TypeOfWork = GetOptionalBundleValue(work.Bundles, BundleCodes.OccurrenceTypeOfWork),
            Versions = dbVersions
        };
        _dbContext.Works.Add(dbWork);

        await _dbContext.SaveChangesAsync(cancellationToken);

        return Ok(asec.ViewModels.Work.FromDbEntity(dbWork));
    }

    private string GetOptionalBundleValue(IList<Bundle> bundles, string bundleCode)
    {
        return bundles?.FirstOrDefault(b => b.Code == bundleCode)?.Values?.FirstOrDefault()?.Value;
    }
}
