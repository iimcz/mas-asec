using Microsoft.AspNetCore.Mvc;
using asec.Compatibility.CollectiveAccess;
using asec.Models;
using asec.Models.Archive;
using asec.Models.Digitalization;

namespace asec.Controllers;

[ApiController]
[Route("/api/v1/export")]
public class ExportController : ControllerBase
{
    private readonly ILogger<ExportController> _logger;
    private readonly EditClient _editClient;
    private readonly AsecDBContext _dbContext;

    private readonly string _storageBaseUrl;

    public ExportController(EditClient editClient, AsecDBContext dbContext, ILogger<ExportController> logger, IConfiguration configuration) : base()
    {
        _editClient = editClient;
        _logger = logger;
        _dbContext = dbContext;
        
        var storageSection = configuration.GetSection("ObjectStorage");
        _storageBaseUrl = $"{storageSection.GetValue<string>("Endpoint")}/{storageSection.GetValue<string>("ArtefactBucket")}";
    }

    [HttpPost("artefact/{id}")]
    public async Task<IActionResult> ExportArtefact(string id, CancellationToken cancellationToken = default(CancellationToken))
    {
        var guid = Guid.Parse(id);
        var artefact = _dbContext.DigitalObjects.OfType<Artefact>().FirstOrDefault(a => a.Id == guid);
        if (artefact == null)
        {
            return NotFound();
        }

        PrepareArtefact(artefact);
        var remoteId = await _editClient.AddOrUpdateDigitalObject(artefact, cancellationToken);
        artefact.RemoteId = remoteId;
        artefact.ExportedAt = DateTime.Now;
        await _dbContext.SaveChangesAsync();

        return Ok(ViewModels.Artefact.FromDBEntity(artefact));
    }

    private void PrepareArtefact(Artefact artefact)
    {
        artefact.InternalNote = "Digitalized version";
        artefact.FedoraUrl = $"{_storageBaseUrl}/{artefact.ObjectId}";
    }
}
