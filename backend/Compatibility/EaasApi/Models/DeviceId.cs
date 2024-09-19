using SQLitePCL;

namespace asec.Compatibility.EaasApi.Models;

public static class DeviceID
{
    public static readonly string ISO = "Q495265";
    public static readonly string Floppy = "Q493576";
    public static readonly string Files = "Q82753";
    public static readonly string Cartridge = "Q633454";

    public static string ToQID(string name)
    {
        return name switch {
            "ISO" => ISO,
            "Floppy" => Floppy,
            "Files" => Files,
            "Cartridge" => Cartridge,
            _ => null
        };
    }

    public static string FromQID(string qid)
    {
        return qid switch {
            "Q495265" => "ISO",
            "Q493576" => "Floppy",
            "Q82753" => "Files",
            "Q633454" => "Cartridge",
            _ => null
        };
    }
}