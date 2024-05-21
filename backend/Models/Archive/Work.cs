using System.ComponentModel.DataAnnotations;

namespace asec.Models.Archive;

public class Work
{
    [Key]
    public Guid Id { get; set; }
    public IList<Status>? Status { get; set; }
    public string Title { get; set; } = "";
    public string AlternativeTitle { get; set; } = "";
    public string Subheading { get; set; } = "";
    public string Description { get; set; } = "";
    public IList<Classification>? Classification { get; set; }
    public DateOnly YearOfPublication { get; set; }
    public IList<Genre>? Genre { get; set; }
    public IList<TimeClassification>? TimeClassification { get; set; }
    public IList<LocationClassification>? LocationClassification { get; set; }
    public string Note { get; set; } = "";
}