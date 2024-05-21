using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace asec.Models.Archive;

[Index(nameof(Time), IsUnique = true)]
public class TimeClassification
{
    [Key]
    public Guid Id { get; set; }
    public string Time { get; set; } = "";
}