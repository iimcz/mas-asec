using System.ComponentModel.DataAnnotations;

namespace asec.Models.Archive;

public class Status
{
    [Key]
    public Guid Id { get; set; }
    public string Value { get; set; } = "";
}