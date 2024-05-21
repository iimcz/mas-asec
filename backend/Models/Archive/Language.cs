using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace asec.Models.Archive;

[Index(nameof(Name), IsUnique = true)]
public class Language
{
    [Key]
    public Guid Id { get; set; }
    public string Name { get; set; } = "";
}