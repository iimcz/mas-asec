using System.ComponentModel.DataAnnotations;

namespace asec.Models.Archive;

public class System
{
    [Key]
    public Guid Id { get; set; }
    public string Name { get; set; } = "";
}