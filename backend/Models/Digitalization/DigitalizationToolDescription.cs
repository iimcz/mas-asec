namespace asec.Models.Digitalization;

public enum PhysicalMediaType
{
    None,
    Floppy5QtInch,
    Floppy3HfInch,
    CD,
    DVD,
    BluRay
}

public class DigitalizationToolDescription
{
    public PhysicalMediaType MediaType { get; set; } = PhysicalMediaType.None;
    public string OutputFormat { get; set; } = String.Empty;
    public string Name { get; set; } = String.Empty;
    public bool Available { get; set; } = false;
    public string Version { get; set; } = String.Empty;
}