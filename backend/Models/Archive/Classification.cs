using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace asec.Models.Archive;

[Index(nameof(Value), IsUnique = true)]
public class Classification
{
    [Key]
    public Guid Id { get; set; }
    public string Value { get; set; } = "";
}