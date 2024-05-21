using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace asec.Models.Archive;

[Index(nameof(Location), IsUnique = true)]
public class LocationClassification
{
    [Key]
    public Guid Id { get; set; }
    public string Location { get; set; } = "";
}