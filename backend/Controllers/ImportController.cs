using asec.ViewModels;
using asec.Compatibility.CollectiveAccess;
using asec.Compatibility.CollectiveAccess.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

using CAWork = asec.Compatibility.CollectiveAccess.Models.Work;
using CAWorkVersion = asec.Compatibility.CollectiveAccess.Models.WorkVersion;

namespace asec.Controllers;

[ApiController]
[Route("/api/v1/import")]
public class ImportController : ControllerBase
{
    private readonly ILogger<ImportController> _logger;
    private readonly SearchClient _searchClient;
    private readonly ItemClient _itemClient;
    private readonly Models.AsecDBContext _dbContext;

    public ImportController(SearchClient searchClient, ItemClient itemClient, Models.AsecDBContext dbContext, ILogger<ImportController> logger)
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
        var result = new List<ViewModels.ImportableWork>();
        foreach (var work in works)
        {
            result.Add(new() {
                Id = work.Id,
                Idno = work.Idno,
                Label = GetOptionalBundleValue(work.Bundles, BundleCodes.OccurrenceLabel),
                CuratorialDescription = GetOptionalBundleValue(work.Bundles, BundleCodes.OccurrenceCuratorialDescription),
                NumVersions = await _itemClient.GetVersionCountForWork(work, cancellationToken),
                IsAlreadyImported = imported.Contains(work.Id)
            });
        }

        return Ok(result);
    }

    [HttpPost]
    public async Task<IActionResult> ImportWorkWithVersions([FromBody] ImportableWork iwork, CancellationToken cancellationToken = default(CancellationToken))
    {
        var work = await _itemClient.GetWork(iwork.Id);
        var versions = await _itemClient.GetVersionsForWork(work);
        var alreadyImported = _dbContext.Works.Include(w => w.Versions).FirstOrDefault(w => w.RemoteId == iwork.Id);

        if (alreadyImported != null)
        {
            return await UpdateExistingWork(alreadyImported, work, versions, cancellationToken);
        }
        else
        {
            return await CreateNewWork(work, versions, cancellationToken);
        }

    }

    [HttpPost("sync/{id}")]
    public async Task<IActionResult> SyncExistingWork(string id, CancellationToken cancellationToken = default(CancellationToken))
    {
        var guid = Guid.Parse(id);
        var work = await _dbContext.Works.Include(w => w.Versions).FirstOrDefaultAsync(w => w.Id == guid);

        if (work == null)
        {
            return NotFound();
        }

        // TODO: handle case where the work on the CA side is deleted
        var caWork = await _itemClient.GetWork(work.RemoteId);
        var caVersions = await _itemClient.GetVersionsForWork(caWork);

        return await UpdateExistingWork(work, caWork, caVersions, cancellationToken);
    }

    private async Task<IActionResult> UpdateExistingWork(Models.Archive.Work dbWork, CAWork work, IList<CAWorkVersion> versions, CancellationToken cancellationToken = default(CancellationToken))
    {
        dbWork.ImportedAt = DateTime.Now;
        dbWork.Label = GetOptionalBundleValue(work.Bundles, BundleCodes.OccurrenceLabel);
        dbWork.InternalNote = GetOptionalBundleValue(work.Bundles, BundleCodes.OccurrenceInternalNote);
        dbWork.TypeOfWork = GetOptionalBundleValue(work.Bundles, BundleCodes.OccurrenceTypeOfWork);

        var remoteIds = versions.Select(v => v.Id).ToHashSet();
        var importedIds = dbWork.Versions.Select(wv => wv.RemoteId).ToHashSet();

        var deletedVersions = dbWork.Versions.Where(wv => !remoteIds.Contains(wv.RemoteId));
        foreach (var workVersion in deletedVersions)
        {
            workVersion.Deleted = true;
        }

        var newVersions = versions.Where(v => !importedIds.Contains(v.Id));
        foreach (var caWorkVersion in newVersions)
        {
            dbWork.Versions.Append(
                new() {
                    RemoteId = caWorkVersion.Id,
                    Label = GetOptionalBundleValue(caWorkVersion.Bundles, BundleCodes.OccurrenceLabel),
                    Description = GetOptionalBundleValue(caWorkVersion.Bundles, BundleCodes.OccurrenceDescription),
                    Subtitle = GetOptionalBundleValue(caWorkVersion.Bundles, BundleCodes.OccurrenceSubtitle),
                    System = GetOptionalBundleValue(caWorkVersion.Bundles, BundleCodes.OccurrenceSystem),
                    CopyProtection = GetOptionalBundleValue(caWorkVersion.Bundles, BundleCodes.OccurrenceCopyProtection),
                    CuratorialDescription = GetOptionalBundleValue(caWorkVersion.Bundles, BundleCodes.OccurrenceCuratorialDescription),
                    InternalNote = GetOptionalBundleValue(caWorkVersion.Bundles, BundleCodes.OccurrenceInternalNote)
                }
            );
        }

        var updatedVersions = versions.Where(v => importedIds.Contains(v.Id)).ToDictionary(v => v.Id);
        foreach (var workVersion in dbWork.Versions)
        {
            var caVersion = updatedVersions.GetValueOrDefault(workVersion.RemoteId);
            if (caVersion == null)
                continue;

            workVersion.Label = GetOptionalBundleValue(caVersion.Bundles, BundleCodes.OccurrenceLabel);
            workVersion.Description = GetOptionalBundleValue(caVersion.Bundles, BundleCodes.OccurrenceDescription);
            workVersion.Subtitle = GetOptionalBundleValue(caVersion.Bundles, BundleCodes.OccurrenceSubtitle);
            workVersion.System = GetOptionalBundleValue(caVersion.Bundles, BundleCodes.OccurrenceSystem);
            workVersion.CopyProtection = GetOptionalBundleValue(caVersion.Bundles, BundleCodes.OccurrenceCopyProtection);
            workVersion.CuratorialDescription = GetOptionalBundleValue(caVersion.Bundles, BundleCodes.OccurrenceCuratorialDescription);
            workVersion.InternalNote = GetOptionalBundleValue(caVersion.Bundles, BundleCodes.OccurrenceInternalNote);
        }

        await _dbContext.SaveChangesAsync();

        return Ok(ViewModels.Work.FromDbEntity(dbWork));
    }

    private async Task<IActionResult> CreateNewWork(CAWork work, IList<CAWorkVersion> versions, CancellationToken cancellationToken = default(CancellationToken))
    {
        _logger.LogInformation("Importing new work, ID: {0}, including {1} versions.", work.Id, versions.Count);

        var dbVersions = new List<Models.Archive.WorkVersion>();
        dbVersions.AddRange(
        versions.Select(wv => new Models.Archive.WorkVersion() {
                RemoteId = wv.Id,
                Label = GetOptionalBundleValue(wv.Bundles, BundleCodes.OccurrenceLabel),
                Description = GetOptionalBundleValue(wv.Bundles, BundleCodes.OccurrenceDescription),
                Subtitle = GetOptionalBundleValue(wv.Bundles, BundleCodes.OccurrenceSubtitle),
                System = GetOptionalBundleValue(wv.Bundles, BundleCodes.OccurrenceSystem),
                CopyProtection = GetOptionalBundleValue(wv.Bundles, BundleCodes.OccurrenceCopyProtection),
                CuratorialDescription = GetOptionalBundleValue(wv.Bundles, BundleCodes.OccurrenceCuratorialDescription),
                InternalNote = GetOptionalBundleValue(wv.Bundles, BundleCodes.OccurrenceInternalNote)
            })
        );

        var dbWork = new Models.Archive.Work() {
            RemoteId = work.Id,
            Label = GetOptionalBundleValue(work.Bundles, BundleCodes.OccurrenceLabel),
            InternalNote = GetOptionalBundleValue(work.Bundles, BundleCodes.OccurrenceInternalNote),
            TypeOfWork = GetOptionalBundleValue(work.Bundles, BundleCodes.OccurrenceTypeOfWork),
            CuratorialDescription = GetOptionalBundleValue(work.Bundles, BundleCodes.OccurrenceCuratorialDescription),
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
