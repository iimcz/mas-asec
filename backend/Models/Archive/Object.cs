using System.ComponentModel.DataAnnotations;

namespace asec.Models.Archive;

public class Object
{
    [Key]
    public Guid Id { get; set; }
    public string Title { get; set; }
    public string AlternativeTitle { get; set; }
    public string Description { get; set; }
    public List<Language> Language { get; set; }
    public string CopyProtection { get; set; }
    public string CopyProtectionNote { get; set; }
    public string EAN { get; set; }
    public string ISBN { get; set; }


    //Data-carrier-type : list
    //Data-carrier-count : number
    //Internal-note : text
    //Storage-location : list
    //Object-status : list
    //License-type : list
}