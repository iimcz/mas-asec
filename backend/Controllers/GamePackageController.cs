using asec.Models;
using asec.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace asec.Controllers;

[ApiController]
[Route("/api/v1/packages")]
public class GamePackageController : ControllerBase
{
    private AsecDBContext _dbContext;

    public GamePackageController(AsecDBContext dbContext)
    {
        _dbContext = dbContext;
    }

    [HttpGet("{packageId}")]
    [Produces(typeof(GamePackage))]
    public async Task<IActionResult> GetGamePackage(string packageId)
    {
        var id = Guid.Parse(packageId);
        var package = await _dbContext.GamePackages
            .Include(p => p.IncludedArtefacts)
            .Include(p => p.Environment)
            .Include(p => p.Version)
            .FirstOrDefaultAsync(p => p.Id == id);
        if (package == null)
            return NotFound();
        return Ok(GamePackage.FromGamePackage(package));
    }

    [HttpPost("{packageId}/emulate")]
    public async Task<IActionResult> EmulateGamePackage(string packageId)
    {
        throw new NotImplementedException();
    }
}