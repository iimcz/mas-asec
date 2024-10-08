using System.Data.Common;
using asec.Compatibility.EaasApi;
using asec.Models;
using asec.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace asec.Controllers;

[ApiController]
[Route("/api/v1/emulators")]
public class EmulatorController : ControllerBase
{
    private AsecDBContext _dbContext;

    public EmulatorController(AsecDBContext dbContext)
    {
        _dbContext = dbContext;
    }

    [HttpGet()]
    public async Task<IActionResult> GetEmulators()
    {
        return Ok(
            (await _dbContext.Environments
                .Include(e => e.Emulator)
                .ThenInclude(em => em.Platforms)
                .ToListAsync()
            ).Select(e => Emulator.FromEmulationEnvironment(e)));
    }
}