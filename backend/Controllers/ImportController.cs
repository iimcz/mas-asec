using asec.ViewModels;
using asec.Compatibility.CollectiveAccess;
using asec.Compatibility.CollectiveAccess.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

using CAWork = asec.Compatibility.CollectiveAccess.Models.Work;
using CAWorkVersion = asec.Compatibility.CollectiveAccess.Models.WorkVersion;
using CAParatext = asec.Compatibility.CollectiveAccess.Models.Paratext;

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
    public async Task<IActionResult> ImportFullWork([FromBody] ImportableWork iwork, CancellationToken cancellationToken = default(CancellationToken))
    {
        var work = await _itemClient.GetWork(iwork.Id);
        var versions = await _itemClient.GetVersionsForWork(work);
        var alreadyImported = _dbContext.Works.Include(w => w.WorkVersions).FirstOrDefault(w => w.RemoteId == iwork.Id);

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
        var work = await _dbContext.Works.Include(w => w.WorkVersions).FirstOrDefaultAsync(w => w.Id == guid);

        if (work == null)
        {
            return NotFound();
        }

        // TODO: handle case where the work on the CA side is deleted
        var caWork = await _itemClient.GetWork(work.RemoteId);
        var caVersions = await _itemClient.GetVersionsForWork(caWork);

        return await UpdateExistingWork(work, caWork, caVersions, cancellationToken);
    }

    private async Task<Models.Archive.Paratext> CreateNewParatext(CAParatext paratext, CancellationToken cancellationToken = default(CancellationToken))
    {
        var physicalObject = (await _itemClient.GetPhysicalObjectsForParatext(paratext, cancellationToken)).FirstOrDefault();
        // physical objects are the last layer, so we don't need another function to handle those.

        return new() {
            RemoteId = paratext.Id,
            Label = GetOptionalBundleValue(paratext.Bundles, BundleCodes.ObjectLabel),
            Language = GetOptionalBundleValue(paratext.Bundles, BundleCodes.OccurrenceLanguage),
            Date = DateOnly.Parse(GetOptionalBundleValue(paratext.Bundles, BundleCodes.OccurrenceDate)),
            InternalNote = GetOptionalBundleValue(paratext.Bundles, BundleCodes.OccurrenceInternalNote),
            FilledOutBy = GetOptionalBundleValue(paratext.Bundles, BundleCodes.OccurrenceFilledOutBy),
            WebsiteUrl = GetOptionalBundleValue(paratext.Bundles, BundleCodes.OccurrenceWebsiteUrl),
            EmissionSize = uint.Parse(GetOptionalBundleValue(paratext.Bundles, BundleCodes.OccurrenceEmissionSize)),
            IdentificationNumber = GetOptionalBundleValue(paratext.Bundles, BundleCodes.OccurrenceIdentificationNumber),
            ParatextType = GetOptionalBundleValue(paratext.Bundles, BundleCodes.OccurrenceParatextType),

            PhysicalObject = physicalObject != null ? new() {
                RemoteId = physicalObject.Id,
                Label = GetOptionalBundleValue(physicalObject.Bundles, BundleCodes.ObjectLabel),
                Description = GetOptionalBundleValue(physicalObject.Bundles, BundleCodes.ObjectDescription),
                Date = DateOnly.Parse(GetOptionalBundleValue(physicalObject.Bundles, BundleCodes.ObjectDate)),
                InternalNote = GetOptionalBundleValue(physicalObject.Bundles, BundleCodes.ObjectInternalNote),
                FilledOutBy = GetOptionalBundleValue(physicalObject.Bundles, BundleCodes.ObjectFilledOutBy),
                PhysicalObjectType = GetOptionalBundleValue(physicalObject.Bundles, BundleCodes.ObjectPhysicalObjectType),
                CountryOfOrigin = GetOptionalBundleValue(physicalObject.Bundles, BundleCodes.ObjectCountryOfOrigin),
                EAN = GetOptionalBundleValue(physicalObject.Bundles, BundleCodes.ObjectEAN),
                ISBN = GetOptionalBundleValue(physicalObject.Bundles, BundleCodes.ObjectISBN),
                Condition = GetOptionalBundleValue(physicalObject.Bundles, BundleCodes.ObjectCondition),
                Location = GetOptionalBundleValue(physicalObject.Bundles, BundleCodes.ObjectLocation),
                Size = GetOptionalBundleValue(physicalObject.Bundles, BundleCodes.ObjectSize),
                Owner = GetOptionalBundleValue(physicalObject.Bundles, BundleCodes.ObjectOwner)
            } : null
        };
    }

    private async Task UpdateExistingParatext(Models.Archive.Paratext dbParatext, CAParatext paratext, CancellationToken cancellationToken = default(CancellationToken))
    {
        var physicalObject = (await _itemClient.GetPhysicalObjectsForParatext(paratext, cancellationToken)).FirstOrDefault();
        if (dbParatext.PhysicalObject == null)
            await _dbContext.Entry(dbParatext).Reference(p => p.PhysicalObject).LoadAsync();

        
        dbParatext.Label = GetOptionalBundleValue(paratext.Bundles, BundleCodes.ObjectLabel);
        dbParatext.Language = GetOptionalBundleValue(paratext.Bundles, BundleCodes.OccurrenceLanguage);
        dbParatext.Date = DateOnly.Parse(GetOptionalBundleValue(paratext.Bundles, BundleCodes.OccurrenceDate));
        dbParatext.InternalNote = GetOptionalBundleValue(paratext.Bundles, BundleCodes.OccurrenceInternalNote);
        dbParatext.FilledOutBy = GetOptionalBundleValue(paratext.Bundles, BundleCodes.OccurrenceFilledOutBy);
        dbParatext.WebsiteUrl = GetOptionalBundleValue(paratext.Bundles, BundleCodes.OccurrenceWebsiteUrl);
        dbParatext.EmissionSize = uint.Parse(GetOptionalBundleValue(paratext.Bundles, BundleCodes.OccurrenceEmissionSize));
        dbParatext.IdentificationNumber = GetOptionalBundleValue(paratext.Bundles, BundleCodes.OccurrenceIdentificationNumber);
        dbParatext.ParatextType = GetOptionalBundleValue(paratext.Bundles, BundleCodes.OccurrenceParatextType);
        dbParatext.ImportedAt = DateTime.Now;


        if (dbParatext.PhysicalObject == null && physicalObject != null)
        {
            dbParatext.PhysicalObject = new() {
                RemoteId = physicalObject.Id,
                Label = GetOptionalBundleValue(physicalObject.Bundles, BundleCodes.ObjectLabel),
                Description = GetOptionalBundleValue(physicalObject.Bundles, BundleCodes.ObjectDescription),
                Date = DateOnly.Parse(GetOptionalBundleValue(physicalObject.Bundles, BundleCodes.ObjectDate)),
                InternalNote = GetOptionalBundleValue(physicalObject.Bundles, BundleCodes.ObjectInternalNote),
                FilledOutBy = GetOptionalBundleValue(physicalObject.Bundles, BundleCodes.ObjectFilledOutBy),
                PhysicalObjectType = GetOptionalBundleValue(physicalObject.Bundles, BundleCodes.ObjectPhysicalObjectType),
                CountryOfOrigin = GetOptionalBundleValue(physicalObject.Bundles, BundleCodes.ObjectCountryOfOrigin),
                EAN = GetOptionalBundleValue(physicalObject.Bundles, BundleCodes.ObjectEAN),
                ISBN = GetOptionalBundleValue(physicalObject.Bundles, BundleCodes.ObjectISBN),
                Condition = GetOptionalBundleValue(physicalObject.Bundles, BundleCodes.ObjectCondition),
                Location = GetOptionalBundleValue(physicalObject.Bundles, BundleCodes.ObjectLocation),
                Size = GetOptionalBundleValue(physicalObject.Bundles, BundleCodes.ObjectSize),
                Owner = GetOptionalBundleValue(physicalObject.Bundles, BundleCodes.ObjectOwner)
            };
        }
        else if (dbParatext.PhysicalObject != null && physicalObject != null)
        {
            dbParatext.PhysicalObject.RemoteId = physicalObject.Id;
            dbParatext.PhysicalObject.Label = GetOptionalBundleValue(physicalObject.Bundles, BundleCodes.ObjectLabel);
            dbParatext.PhysicalObject.Description = GetOptionalBundleValue(physicalObject.Bundles, BundleCodes.ObjectDescription);
            dbParatext.PhysicalObject.Date = DateOnly.Parse(GetOptionalBundleValue(physicalObject.Bundles, BundleCodes.ObjectDate));
            dbParatext.PhysicalObject.InternalNote = GetOptionalBundleValue(physicalObject.Bundles, BundleCodes.ObjectInternalNote);
            dbParatext.PhysicalObject.FilledOutBy = GetOptionalBundleValue(physicalObject.Bundles, BundleCodes.ObjectFilledOutBy);
            dbParatext.PhysicalObject.PhysicalObjectType = GetOptionalBundleValue(physicalObject.Bundles, BundleCodes.ObjectPhysicalObjectType);
            dbParatext.PhysicalObject.CountryOfOrigin = GetOptionalBundleValue(physicalObject.Bundles, BundleCodes.ObjectCountryOfOrigin);
            dbParatext.PhysicalObject.EAN = GetOptionalBundleValue(physicalObject.Bundles, BundleCodes.ObjectEAN);
            dbParatext.PhysicalObject.ISBN = GetOptionalBundleValue(physicalObject.Bundles, BundleCodes.ObjectISBN);
            dbParatext.PhysicalObject.Condition = GetOptionalBundleValue(physicalObject.Bundles, BundleCodes.ObjectCondition);
            dbParatext.PhysicalObject.Location = GetOptionalBundleValue(physicalObject.Bundles, BundleCodes.ObjectLocation);
            dbParatext.PhysicalObject.Size = GetOptionalBundleValue(physicalObject.Bundles, BundleCodes.ObjectSize);
            dbParatext.PhysicalObject.Owner = GetOptionalBundleValue(physicalObject.Bundles, BundleCodes.ObjectOwner);
        }
        else if (dbParatext.PhysicalObject != null && physicalObject == null)
        {
            dbParatext.PhysicalObject.Deleted = true;
        }
    }

    private async Task<Models.Archive.WorkVersion> CreateNewVersion(CAWorkVersion version, CancellationToken cancellationToken = default(CancellationToken))
    {
        var paratexts = await _itemClient.GetParatextsForVersion(version, cancellationToken);
        var paratextTasks = paratexts.Select(async pt => await CreateNewParatext(pt, cancellationToken));
        await Task.WhenAll(paratextTasks);

        return new() {
            RemoteId = version.Id,
            Label = GetOptionalBundleValue(version.Bundles, BundleCodes.OccurrenceLabel),
            Description = GetOptionalBundleValue(version.Bundles, BundleCodes.OccurrenceDescription),
            Subtitle = GetOptionalBundleValue(version.Bundles, BundleCodes.OccurrenceSubtitle),
            System = GetOptionalBundleValue(version.Bundles, BundleCodes.OccurrenceSystem),
            CopyProtection = GetOptionalBundleValue(version.Bundles, BundleCodes.OccurrenceCopyProtection),
            CuratorialDescription = GetOptionalBundleValue(version.Bundles, BundleCodes.OccurrenceCuratorialDescription),
            InternalNote = GetOptionalBundleValue(version.Bundles, BundleCodes.OccurrenceInternalNote),

            Paratexts = new List<Models.Archive.Paratext>(paratextTasks.Select(pt => pt.Result))
        };
    }

    private async Task UpdateExistingVersion(Models.Archive.WorkVersion dbVersion, CAWorkVersion version, CancellationToken cancellationToken = default(CancellationToken))
    {
        if (dbVersion.Paratexts == null)
            await _dbContext.Entry(dbVersion).Collection(v => v.Paratexts).LoadAsync();
        if (dbVersion.Paratexts == null)
            dbVersion.Paratexts = new List<Models.Archive.Paratext>();

        dbVersion.Label = GetOptionalBundleValue(version.Bundles, BundleCodes.OccurrenceLabel);
        dbVersion.Description = GetOptionalBundleValue(version.Bundles, BundleCodes.OccurrenceDescription);
        dbVersion.Subtitle = GetOptionalBundleValue(version.Bundles, BundleCodes.OccurrenceSubtitle);
        dbVersion.System = GetOptionalBundleValue(version.Bundles, BundleCodes.OccurrenceSystem);
        dbVersion.CopyProtection = GetOptionalBundleValue(version.Bundles, BundleCodes.OccurrenceCopyProtection);
        dbVersion.CuratorialDescription = GetOptionalBundleValue(version.Bundles, BundleCodes.OccurrenceCuratorialDescription);
        dbVersion.InternalNote = GetOptionalBundleValue(version.Bundles, BundleCodes.OccurrenceInternalNote);

        dbVersion.ImportedAt = DateTime.Now;

        var paratexts = await _itemClient.GetParatextsForVersion(version, cancellationToken);
        
        var remoteIds = paratexts.Select(p => p.Id).ToHashSet();
        var importedIds = dbVersion.Paratexts.Select(p => p.RemoteId).ToHashSet();

        var deletedParatexts = dbVersion.Paratexts.Where(p => !remoteIds.Contains(p.RemoteId));
        foreach (var paratext in deletedParatexts)
        {
            // It is possible a paratext is created by us and not yet exported into CA.
            // In that case, do not mark it as deleted - it was not in CA in the first place.
            if (paratext.RemoteId < 0)
                continue;
            paratext.Deleted = true;
        }

        var newParatexts = paratexts.Where(p => !importedIds.Contains(p.Id));
        foreach (var paratext in newParatexts)
        {
            dbVersion.Paratexts.Add(await CreateNewParatext(paratext, cancellationToken));
        }

        var updatedParatexts = paratexts.Where(p => importedIds.Contains(p.Id)).ToDictionary(p => p.Id);
        foreach (var paratext in dbVersion.Paratexts)
        {
            var caParatext = updatedParatexts.GetValueOrDefault(paratext.RemoteId);
            if (caParatext == null)
                continue;

            await UpdateExistingParatext(paratext, caParatext, cancellationToken);
        }
    }

    private async Task<IActionResult> UpdateExistingWork(Models.Archive.Work dbWork, CAWork work, IList<CAWorkVersion> versions, CancellationToken cancellationToken = default(CancellationToken))
    {
        dbWork.ImportedAt = DateTime.Now;
        dbWork.Label = GetOptionalBundleValue(work.Bundles, BundleCodes.OccurrenceLabel);
        dbWork.InternalNote = GetOptionalBundleValue(work.Bundles, BundleCodes.OccurrenceInternalNote);
        dbWork.TypeOfWork = GetOptionalBundleValue(work.Bundles, BundleCodes.OccurrenceTypeOfWork);

        var remoteIds = versions.Select(v => v.Id).ToHashSet();
        var importedIds = dbWork.WorkVersions.Select(wv => wv.RemoteId).ToHashSet();

        var deletedVersions = dbWork.WorkVersions.Where(wv => !remoteIds.Contains(wv.RemoteId));
        foreach (var workVersion in deletedVersions)
        {
            workVersion.Deleted = true;
        }

        var newVersions = versions.Where(v => !importedIds.Contains(v.Id));
        foreach (var caWorkVersion in newVersions)
        {
            dbWork.WorkVersions.Add(await CreateNewVersion(caWorkVersion));
        }

        var updatedVersions = versions.Where(v => importedIds.Contains(v.Id)).ToDictionary(v => v.Id);
        foreach (var workVersion in dbWork.WorkVersions)
        {
            var caVersion = updatedVersions.GetValueOrDefault(workVersion.RemoteId);
            if (caVersion == null)
                continue;

            await UpdateExistingVersion(workVersion, caVersion, cancellationToken);
        }

        await _dbContext.SaveChangesAsync();

        return Ok(ViewModels.Work.FromDbEntity(dbWork));
    }

    private async Task<IActionResult> CreateNewWork(CAWork work, IList<CAWorkVersion> versions, CancellationToken cancellationToken = default(CancellationToken))
    {
        _logger.LogInformation("Importing new work, ID: {0}, including {1} versions.", work.Id, versions.Count);

        var dbVersions = new List<Models.Archive.WorkVersion>();
        var versionTasks = versions.Select(async wv => await CreateNewVersion(wv, cancellationToken));
        await Task.WhenAll(versionTasks);
        dbVersions.AddRange(versionTasks.Select(vt => vt.Result));

        var dbWork = new Models.Archive.Work() {
            RemoteId = work.Id,
            Label = GetOptionalBundleValue(work.Bundles, BundleCodes.OccurrenceLabel),
            InternalNote = GetOptionalBundleValue(work.Bundles, BundleCodes.OccurrenceInternalNote),
            TypeOfWork = GetOptionalBundleValue(work.Bundles, BundleCodes.OccurrenceTypeOfWork),
            CuratorialDescription = GetOptionalBundleValue(work.Bundles, BundleCodes.OccurrenceCuratorialDescription),
            WorkVersions = dbVersions
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
