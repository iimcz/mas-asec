using System.ComponentModel.DataAnnotations;

namespace asec.Models.Archive;

public class Work
{
    [Key]
    public Guid Id { get; set; }
    public IEnumerable<Status> Status { get; set; }
    public string Title { get; set; } = "";
    public string AlternativeTitle { get; set; } = "";
    public string Subheading { get; set; } = "";
    public string Description { get; set; } = "";
    public IEnumerable<Classification> Classification { get; set; }
    public string YearOfPublication { get; set; } = "";
    public IEnumerable<Genre> Genre { get; set; }
    public IEnumerable<TimeClassification> TimeClassification { get; set; }
    public IEnumerable<LocationClassification> LocationClassification { get; set; }
    public IEnumerable<Version> Versions { get; set; }
    public IEnumerable<Paratext> Paratexts { get; set; }
    public string Note { get; set; } = "";
}