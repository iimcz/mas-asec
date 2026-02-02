namespace asec.ViewModels;

public record ImportableWork
{
    public int Id {get; set;} = 0;
    public string Idno {get; set;} = "";
    public string Label {get; set;} = "";
    public int NumVersions {get; set;} = 0;
    public bool IsAlreadyImported {get; set;} = false;
}
