using Microsoft.EntityFrameworkCore;

namespace asec.Models.Digitalization;

[Index("Hash")]
public class DigitalizationTool
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    public string Version { get; set; }
    public string Environment { get; set; }
    // TODO: maybe use an enum here as well enum?
    public string PhysicalMedia { get; set; }
    public string Hash { get; set; }
}