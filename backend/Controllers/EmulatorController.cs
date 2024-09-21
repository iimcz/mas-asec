using asec.Compatibility.EaasApi;
using asec.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace asec.Controllers;

[ApiController]
[Route("/api/v1/emulators")]
public class EmulatorController : ControllerBase
{
    EmulatorRepositoryClient _eaasClient;

    public EmulatorController(IConfiguration config)
    {
        _eaasClient = new(config);
        
        var emulatorsSection = config.GetSection("Emulators");
    }


    [HttpGet]
    public async Task<IActionResult> GetEmulators()
    {
        throw new NotImplementedException();
        //var eaasData = await _eaasClient.GetEmulators();
        //var result = eaasData?.Select(
        //    e => new Emulator(e.name, e.version)
        //);
        //return Ok(result);
    }
}