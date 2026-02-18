using Microsoft.AspNetCore.Mvc;
using asec.Models;

namespace asec.Controllers;

[ApiController]
[Route("/api/v1/router")]
public class RouterController : ControllerBase
{
    private const string CONFIG_SECTION = "Router";
    private const string CA_SECTION = "CollectiveAccess";

    private readonly string _caUiBaseUrl;
    private readonly int _workTypeId;
    private readonly int _workVersionTypeId;
    private readonly int _physicalObjectTypeId;

    private readonly AsecDBContext _dbContext;

    public RouterController(AsecDBContext dbContext, IConfiguration configuration) : base()
    {
        var section = configuration.GetSection(CONFIG_SECTION);

        var caSection = section.GetSection(CA_SECTION);
        _caUiBaseUrl = caSection.GetValue<string>("UIBaseUrl");
        _workTypeId = caSection.GetValue<int>("WorkTypeId");
        _workVersionTypeId = caSection.GetValue<int>("WorkVersionTypeId");
        _physicalObjectTypeId = caSection.GetValue<int>("PhysicalObjectTypeId");

        _dbContext = dbContext;
    }

    [HttpGet("ca/home")]
    public IActionResult CAGoHome()
    {
        return Redirect($"{_caUiBaseUrl}index.php");
    }

    [HttpGet("ca/work/add")]
    public IActionResult CAAddWork()
    {
        return Redirect($"{_caUiBaseUrl}index.php/editor/occurrences/OccurrenceEditor/Edit/type_id/{_workTypeId}");
    }

    [HttpGet("ca/work/edit/{id}")]
    public async Task<IActionResult> CAEditWork(string id)
    {
        var guid = Guid.Parse(id);
        var work = await _dbContext.Works.FindAsync(guid);

        if (work == null)
        {
            return NotFound();
        }

        return Redirect($"{_caUiBaseUrl}index.php/editor/occurrences/OccurrenceEditor/Edit/occurrence_id/{work.RemoteId}");
    }

    [HttpGet("ca/workversion/add")]
    public IActionResult CAAddWorkVersion()
    {
        return Redirect($"{_caUiBaseUrl}index.php/editor/occurrences/OccurrenceEditor/Edit/type_id/{_workVersionTypeId}");
    }

    [HttpGet("ca/workversion/edit/{id}")]
    public async Task<IActionResult> CAEditWorkVersion(string id)
    {
        var guid = Guid.Parse(id);
        var workVersion = await _dbContext.WorkVersions.FindAsync(guid);

        if (workVersion == null)
        {
            return NotFound();
        }

        return Redirect($"{_caUiBaseUrl}index.php/editor/occurrences/OccurrenceEditor/Edit/occurrence_id/{workVersion.RemoteId}");
    }

    [HttpGet("ca/physicalobject/add")]
    public IActionResult CAAddPhysicalObject()
    {
        return Redirect($"{_caUiBaseUrl}index.php/editor/objects/ObjectEditor/Edit/type_id/{_physicalObjectTypeId}");
    }

    [HttpGet("ca/physicalobject/edit/{id}")]
    public async Task<IActionResult> CAEditPhysicalObject(string id)
    {
        var guid = Guid.Parse(id);
        var pobject = await _dbContext.PhysicalObjects.FindAsync(guid);

        if (pobject == null)
        {
            return NotFound();
        }

        return Redirect($"{_caUiBaseUrl}index.php/editor/objects/ObjectEditor/Edit/object_id/{pobject.RemoteId}");
    }
}
