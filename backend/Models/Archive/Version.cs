using System.ComponentModel.DataAnnotations;
using asec.Models.Digitalization;
using asec.Models.Emulation;

namespace asec.Models.Archive;

public class Version
{
    [Key]
    public Guid Id { get; set; }
    public Work Work { get; set; }
    public IEnumerable<Status> Status { get; set; }
    public string Title { get; set; } = "";
    public string AlternativeTitle { get; set; } = "";
    public string YearOfPublication { get; set; } = "";
    public IEnumerable<System> System { get; set; }
    public string SystemRequirements { get; set; } = "";
    public string Note { get; set; } = "";
    public IEnumerable<Artefact> Artefacts { get; set; }
    public IEnumerable<GamePackage> GamePackages { get; set; }

}