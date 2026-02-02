using asec.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace asec.Controllers;

[ApiController]
[Route("/api/v1/import")]
public class ImportController : ControllerBase
{
    public ImportController()
    {

    }

    [HttpGet("available")]
    public async Task<IActionResult> GetAvailableWorks([FromQuery] string q)
    {
        // TODO: return actual results
        return Ok(new List<ImportableWork> {
                new ImportableWork {
                    Id = 0,
                    Idno = "testament",
                    Label = "Testament",
                    NumVersions = 1,
                    IsAlreadyImported = false
                },
                new ImportableWork {
                    Id = 1,
                    Idno = "vlak",
                    Label = "Vlak",
                    NumVersions = 1,
                    IsAlreadyImported = true
                }
                });
    }

    [HttpPost]
    public async Task<IActionResult> ImportWorkWithVersions([FromBody] ImportableWork work)
    {
        // TODO: implement
        return Ok();
    }
}
