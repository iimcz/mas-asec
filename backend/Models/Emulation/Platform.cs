using System.ComponentModel.DataAnnotations;
using asec.Models.Digitalization;

namespace asec.Models.Emulation;

public class Platform
{
    [Key]
    public string Name { get; set; }
    public IList<PhysicalMediaType> MediaTypes { get; set; }
}