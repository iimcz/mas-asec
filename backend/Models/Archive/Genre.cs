using System.ComponentModel.DataAnnotations;

namespace asec.Models.Archive;

public class Genre
{
    [Key]
    public Guid Id { get; set; }
    public string Name { get; set; } = "";
}