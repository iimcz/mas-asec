using System.ComponentModel.DataAnnotations;

namespace asec.Models.Archive;

public class Version
{
    [Key]
    public Guid Id { get; set; }
    public Work? Work { get; set; }
    public IList<Status>? Status { get; set; }
    public string? Title { get; set; }
    public string? AlternativeTitle { get; set; }
    public DateOnly YearOfPublication { get; set; }
    public IList<System>? System { get; set; }
    public string? SystemRequirements { get; set; }
    public string? Note { get; set; } 

}