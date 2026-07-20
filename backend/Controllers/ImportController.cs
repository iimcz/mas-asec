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
    private readonly ListClient _listClient;
    private readonly Models.AsecDBContext _dbContext;

    private readonly int _digiObjectFormatsListId;

    public ImportController(SearchClient searchClient, ItemClient itemClient, ListClient listClient, Models.AsecDBContext dbContext, IConfiguration configuration, ILogger<ImportController> logger)
    {
        _searchClient = searchClient;
        _itemClient = itemClient;
        _listClient = listClient;
        _dbContext = dbContext;
        _logger = logger;

        _digiObjectFormatsListId = configuration.GetSection("CollectiveAccessAPI").GetValue<int>("DigitalObjectFormatListId");
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
            result.Add(new()
            {
                Id = work.Id,
                Idno = work.Idno,
                Label = work.Bundles.GetOptionalBundleValue(BundleCodes.OccurrenceLabel),
                CuratorialDescription = work.Bundles.GetOptionalBundleValue(BundleCodes.OccurrenceCuratorialDescription),
                NumVersions = await _itemClient.GetVersionCountForWork(work, cancellationToken),
                IsAlreadyImported = imported.Contains(work.Id)
            });
        }

        return Ok(result);
    }

    [HttpGet("digiobj-listoptions")]
    [Produces(typeof(ViewModels.ArtefactListOptions))]
    public async Task<IActionResult> GetDigitalObjectListOptions(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Querying CA for defined lists used in digital objects.");

        // Right now we only have the digital object format as a dynamic list, so query that.
        // If we ever need more, there should be a list defined somewhere we would iterate
        // through here.
        var formatList = await _listClient.GetListItems(_digiObjectFormatsListId, cancellationToken);
        var formats = new Dictionary<string, string>();
        foreach (var format in formatList)
        {
            formats.Add(
                format.Bundles.GetOptionalBundleValue(BundleCodes.ListItemsItemValue),
                format.Bundles.GetOptionalBundleValue(BundleCodes.ListItemsPreferredLabel)
            );
        }

        return Ok(new ViewModels.ArtefactListOptions {
            Formats = formats
        });
    }

    [HttpPost("full")]
    public async Task<IActionResult> ImportFullWork([FromBody] ImportableWork iwork, CancellationToken cancellationToken = default(CancellationToken))
    {
        var work = await _itemClient.GetWork(iwork.Id, cancellationToken);
        var versions = await _itemClient.GetVersionsForWork(work, cancellationToken);
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
        var work = await _dbContext.Works.Include(w => w.WorkVersions).FirstOrDefaultAsync(w => w.Id == guid, cancellationToken: cancellationToken);

        if (work == null)
        {
            return NotFound();
        }

        // TODO: handle case where the work on the CA side is deleted
        var caWork = await _itemClient.GetWork(work.RemoteId, cancellationToken);
        var caVersions = await _itemClient.GetVersionsForWork(caWork, cancellationToken);

        return await UpdateExistingWork(work, caWork, caVersions, cancellationToken);
    }

    private async Task<Models.Archive.Paratext> CreateNewParatext(CAParatext paratext, CancellationToken cancellationToken = default(CancellationToken))
    {
        var physicalObjects = (await _itemClient.GetPhysicalObjectsForParatext(paratext, cancellationToken)).ToList();
        // physical objects are the last layer, so we don't need another function to handle those.

        IList<Models.Archive.PhysicalObject> importPhysicalObjects = [];
        foreach (var physObject in physicalObjects)
        {
            importPhysicalObjects.Add(new()
            {
                RemoteId = physObject.Id,
                Label = physObject.Bundles.GetOptionalBundleValue(BundleCodes.ObjectLabel),
                Description = physObject.Bundles.GetOptionalBundleValue(BundleCodes.ObjectDescription),
                Date = DateOnly.Parse(physObject.Bundles.GetOptionalBundleValue(BundleCodes.ObjectDate)),
                InternalNote = physObject.Bundles.GetOptionalBundleValue(BundleCodes.ObjectInternalNote),
                FilledOutBy = physObject.Bundles.GetOptionalBundleValue(BundleCodes.ObjectFilledOutBy),
                PhysicalObjectType = physObject.Bundles.GetOptionalBundleValue(BundleCodes.ObjectPhysicalObjectType),
                CountryOfOrigin = physObject.Bundles.GetOptionalBundleValue(BundleCodes.ObjectCountryOfOrigin),
                EAN = physObject.Bundles.GetOptionalBundleValue(BundleCodes.ObjectEAN),
                ISBN = physObject.Bundles.GetOptionalBundleValue(BundleCodes.ObjectISBN),
                Condition = physObject.Bundles.GetOptionalBundleValue(BundleCodes.ObjectCondition),
                Location = physObject.Bundles.GetOptionalBundleValue(BundleCodes.ObjectLocation),
                Size = physObject.Bundles.GetOptionalBundleValue(BundleCodes.ObjectSize),
                Owner = physObject.Bundles.GetOptionalBundleValue(BundleCodes.ObjectOwner)
            });
        }

        return new()
        {
            RemoteId = paratext.Id,
            Label = paratext.Bundles.GetOptionalBundleValue(BundleCodes.ObjectLabel),
            Language = paratext.Bundles.GetOptionalBundleValue(BundleCodes.OccurrenceLanguage),
            Date = paratext.Bundles.GetOptionalBundleValue(BundleCodes.OccurrenceDate),
            InternalNote = paratext.Bundles.GetOptionalBundleValue(BundleCodes.OccurrenceInternalNote),
            FilledOutBy = paratext.Bundles.GetOptionalBundleValue(BundleCodes.OccurrenceFilledOutBy),
            WebsiteUrl = paratext.Bundles.GetOptionalBundleValue(BundleCodes.OccurrenceWebsiteUrl),
            EmissionSize = paratext.Bundles.GetOptionalBundleUintValue(BundleCodes.OccurrenceEmissionSize) ?? 0,
            IdentificationNumber = paratext.Bundles.GetOptionalBundleValue(BundleCodes.OccurrenceIdentificationNumber),
            ParatextType = paratext.Bundles.GetOptionalBundleValue(BundleCodes.OccurrenceParatextType),

            PhysicalObjects = importPhysicalObjects
        };
    }

    private async Task UpdateExistingParatext(Models.Archive.Paratext dbParatext, CAParatext paratext, CancellationToken cancellationToken = default(CancellationToken))
    {
        var physicalObjects = (await _itemClient.GetPhysicalObjectsForParatext(paratext, cancellationToken)).ToList();
        if (dbParatext.PhysicalObjects == null)
            await _dbContext.Entry(dbParatext).Collection(p => p.PhysicalObjects).LoadAsync(cancellationToken);


        dbParatext.Label = paratext.Bundles.GetOptionalBundleValue(BundleCodes.ObjectLabel);
        dbParatext.Language = paratext.Bundles.GetOptionalBundleValue(BundleCodes.OccurrenceLanguage);
        dbParatext.Date = paratext.Bundles.GetOptionalBundleValue(BundleCodes.OccurrenceDate);
        dbParatext.InternalNote = paratext.Bundles.GetOptionalBundleValue(BundleCodes.OccurrenceInternalNote);
        dbParatext.FilledOutBy = paratext.Bundles.GetOptionalBundleValue(BundleCodes.OccurrenceFilledOutBy);
        dbParatext.WebsiteUrl = paratext.Bundles.GetOptionalBundleValue(BundleCodes.OccurrenceWebsiteUrl);
        dbParatext.EmissionSize = paratext.Bundles.GetOptionalBundleUintValue(BundleCodes.OccurrenceEmissionSize) ?? 0;
        dbParatext.IdentificationNumber = paratext.Bundles.GetOptionalBundleValue(BundleCodes.OccurrenceIdentificationNumber);
        dbParatext.ParatextType = paratext.Bundles.GetOptionalBundleValue(BundleCodes.OccurrenceParatextType);
        dbParatext.ImportedAt = DateTime.Now;

        foreach (var dbPhysObject in dbParatext.PhysicalObjects!)
        {
            if (!physicalObjects.Exists(p => p.Id == dbPhysObject.RemoteId))
                dbPhysObject.Deleted = true;
        }

        foreach (var physObject in physicalObjects)
        {
            var existingPhysicalObject = dbParatext.PhysicalObjects!.Where(p => p.RemoteId == physObject.Id).FirstOrDefault();

            if (existingPhysicalObject != null)
            {
                existingPhysicalObject.RemoteId = physObject.Id;
                existingPhysicalObject.Label = physObject.Bundles.GetOptionalBundleValue(BundleCodes.ObjectLabel);
                existingPhysicalObject.Description = physObject.Bundles.GetOptionalBundleValue(BundleCodes.ObjectDescription);
                existingPhysicalObject.Date = DateOnly.Parse(physObject.Bundles.GetOptionalBundleValue(BundleCodes.ObjectDate));
                existingPhysicalObject.InternalNote = physObject.Bundles.GetOptionalBundleValue(BundleCodes.ObjectInternalNote);
                existingPhysicalObject.FilledOutBy = physObject.Bundles.GetOptionalBundleValue(BundleCodes.ObjectFilledOutBy);
                existingPhysicalObject.PhysicalObjectType = physObject.Bundles.GetOptionalBundleValue(BundleCodes.ObjectPhysicalObjectType);
                existingPhysicalObject.CountryOfOrigin = physObject.Bundles.GetOptionalBundleValue(BundleCodes.ObjectCountryOfOrigin);
                existingPhysicalObject.EAN = physObject.Bundles.GetOptionalBundleValue(BundleCodes.ObjectEAN);
                existingPhysicalObject.ISBN = physObject.Bundles.GetOptionalBundleValue(BundleCodes.ObjectISBN);
                existingPhysicalObject.Condition = physObject.Bundles.GetOptionalBundleValue(BundleCodes.ObjectCondition);
                existingPhysicalObject.Location = physObject.Bundles.GetOptionalBundleValue(BundleCodes.ObjectLocation);
                existingPhysicalObject.Size = physObject.Bundles.GetOptionalBundleValue(BundleCodes.ObjectSize);
                existingPhysicalObject.Owner = physObject.Bundles.GetOptionalBundleValue(BundleCodes.ObjectOwner);
            }
            else
            {
                dbParatext.PhysicalObjects!.Add(new() {
                    RemoteId = physObject.Id,
                    Label = physObject.Bundles.GetOptionalBundleValue(BundleCodes.ObjectLabel),
                    Description = physObject.Bundles.GetOptionalBundleValue(BundleCodes.ObjectDescription),
                    Date = DateOnly.Parse(physObject.Bundles.GetOptionalBundleValue(BundleCodes.ObjectDate)),
                    InternalNote = physObject.Bundles.GetOptionalBundleValue(BundleCodes.ObjectInternalNote),
                    FilledOutBy = physObject.Bundles.GetOptionalBundleValue(BundleCodes.ObjectFilledOutBy),
                    PhysicalObjectType = physObject.Bundles.GetOptionalBundleValue(BundleCodes.ObjectPhysicalObjectType),
                    CountryOfOrigin = physObject.Bundles.GetOptionalBundleValue(BundleCodes.ObjectCountryOfOrigin),
                    EAN = physObject.Bundles.GetOptionalBundleValue(BundleCodes.ObjectEAN),
                    ISBN = physObject.Bundles.GetOptionalBundleValue(BundleCodes.ObjectISBN),
                    Condition = physObject.Bundles.GetOptionalBundleValue(BundleCodes.ObjectCondition),
                    Location = physObject.Bundles.GetOptionalBundleValue(BundleCodes.ObjectLocation),
                    Size = physObject.Bundles.GetOptionalBundleValue(BundleCodes.ObjectSize),
                    Owner = physObject.Bundles.GetOptionalBundleValue(BundleCodes.ObjectOwner)
                });
            }
        }
    }

    private async Task<Models.Archive.WorkVersion> CreateNewVersion(CAWorkVersion version, CancellationToken cancellationToken = default(CancellationToken))
    {
        var paratexts = await _itemClient.GetParatextsForVersion(version, cancellationToken);
        var paratextTasks = paratexts.Select(async pt => await CreateNewParatext(pt, cancellationToken));
        await Task.WhenAll(paratextTasks);

        return new()
        {
            RemoteId = version.Id,
            Label = version.Bundles.GetOptionalBundleValue(BundleCodes.OccurrenceLabel),
            Description = version.Bundles.GetOptionalBundleValue(BundleCodes.OccurrenceDescription),
            Subtitle = version.Bundles.GetOptionalBundleValue(BundleCodes.OccurrenceSubtitle),
            System = version.Bundles.GetOptionalBundleValue(BundleCodes.OccurrenceSystem),
            CopyProtection = version.Bundles.GetOptionalBundleValue(BundleCodes.OccurrenceCopyProtection),
            CuratorialDescription = version.Bundles.GetOptionalBundleValue(BundleCodes.OccurrenceCuratorialDescription),
            InternalNote = version.Bundles.GetOptionalBundleValue(BundleCodes.OccurrenceInternalNote),

            Paratexts = new List<Models.Archive.Paratext>(paratextTasks.Select(pt => pt.Result))
        };
    }

    private async Task UpdateExistingVersion(Models.Archive.WorkVersion dbVersion, CAWorkVersion version, CancellationToken cancellationToken = default(CancellationToken))
    {
        if (dbVersion.Paratexts == null)
            await _dbContext.Entry(dbVersion).Collection(v => v.Paratexts).LoadAsync(cancellationToken);
        if (dbVersion.Paratexts == null)
            dbVersion.Paratexts = new List<Models.Archive.Paratext>();

        dbVersion.Label = version.Bundles.GetOptionalBundleValue(BundleCodes.OccurrenceLabel);
        dbVersion.Description = version.Bundles.GetOptionalBundleValue(BundleCodes.OccurrenceDescription);
        dbVersion.Subtitle = version.Bundles.GetOptionalBundleValue(BundleCodes.OccurrenceSubtitle);
        dbVersion.System = version.Bundles.GetOptionalBundleValue(BundleCodes.OccurrenceSystem);
        dbVersion.CopyProtection = version.Bundles.GetOptionalBundleValue(BundleCodes.OccurrenceCopyProtection);
        dbVersion.CuratorialDescription = version.Bundles.GetOptionalBundleValue(BundleCodes.OccurrenceCuratorialDescription);
        dbVersion.InternalNote = version.Bundles.GetOptionalBundleValue(BundleCodes.OccurrenceInternalNote);

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
        dbWork.Label = work.Bundles.GetOptionalBundleValue(BundleCodes.OccurrenceLabel);
        dbWork.InternalNote = work.Bundles.GetOptionalBundleValue(BundleCodes.OccurrenceInternalNote);
        dbWork.TypeOfWork = work.Bundles.GetOptionalBundleValue(BundleCodes.OccurrenceTypeOfWork);

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
            dbWork.WorkVersions.Add(await CreateNewVersion(caWorkVersion, cancellationToken));
        }

        var updatedVersions = versions.Where(v => importedIds.Contains(v.Id)).ToDictionary(v => v.Id);
        foreach (var workVersion in dbWork.WorkVersions)
        {
            var caVersion = updatedVersions.GetValueOrDefault(workVersion.RemoteId);
            if (caVersion == null)
                continue;

            await UpdateExistingVersion(workVersion, caVersion, cancellationToken);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        return Ok(ViewModels.Work.FromDbEntity(dbWork));
    }

    private async Task<IActionResult> CreateNewWork(CAWork work, IList<CAWorkVersion> versions, CancellationToken cancellationToken = default(CancellationToken))
    {
        _logger.LogInformation("Importing new work, ID: {0}, including {1} versions.", work.Id, versions.Count);

        var dbVersions = new List<Models.Archive.WorkVersion>();
        var versionTasks = versions.Select(async wv => await CreateNewVersion(wv, cancellationToken));
        await Task.WhenAll(versionTasks);
        dbVersions.AddRange(versionTasks.Select(vt => vt.Result));

        var dbWork = new Models.Archive.Work()
        {
            RemoteId = work.Id,
            Label = work.Bundles.GetOptionalBundleValue(BundleCodes.OccurrenceLabel),
            InternalNote = work.Bundles.GetOptionalBundleValue(BundleCodes.OccurrenceInternalNote),
            TypeOfWork = work.Bundles.GetOptionalBundleValue(BundleCodes.OccurrenceTypeOfWork),
            CuratorialDescription = work.Bundles.GetOptionalBundleValue(BundleCodes.OccurrenceCuratorialDescription),
            WorkVersions = dbVersions
        };
        _dbContext.Works.Add(dbWork);

        await _dbContext.SaveChangesAsync(cancellationToken);

        return Ok(asec.ViewModels.Work.FromDbEntity(dbWork));
    }
}
