using asec.Models.Emulation;

namespace asec.Models.Archive;

public class Paratext
{
    public Guid Id { get; set; }
    public Work Work { get; set; }
    public Version Version { get; set; }
    public GamePackage GamePackage { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public string Source { get; set; }
    public string SourceUrl { get; set; }
    public bool Downloadable { get; set; }
    public string Filename { get; set; }
    public string Thumbnail { get; set; }
}