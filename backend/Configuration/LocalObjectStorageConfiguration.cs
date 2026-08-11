using System.ComponentModel.DataAnnotations;

namespace asec.Configuration;

public class LocalObjectStorageConfiguration
{
    [Required]
    public string Endpoint { get; set; }

    [Required]
    public string AccessKey { get; set; }

    [Required]
    public string SecretKey { get; set; }

    [Required]
    public string DigitalObjectBucket { get; set; }

    [Required]
    public string ArtefactFolder { get; set; }

    [Required]
    public string PlayableFolder { get; set; }

    [Required]
    public string ModificationFolder { get; set; }

    [Required]
    public string ParatextFolder { get; set; }

    [Required]
    public string CacheDir { get; set; }
}
