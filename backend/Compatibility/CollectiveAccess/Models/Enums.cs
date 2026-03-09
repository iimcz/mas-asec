namespace asec.Compatibility.CollectiveAccess.Models;

public struct BundleCodes
{
    public static readonly string OccurrenceInternalNote = "ca_occurrences.internal_notes";
    public static readonly string OccurrenceTypeOfWork = "ca_occurrences.type_of_work";
    public static readonly string OccurrenceLabel = "ca_occurrences.preferred_labels";
    public static readonly string OccurrenceId = "ca_occurrences.occurrence_id";
    public static readonly string OccurrenceDescription = "ca_occurrences.description";
    public static readonly string OccurrenceSubtitle = "ca_occurrences.subtitle";
    public static readonly string OccurrenceSystem = "ca_occurrences.system";
    public static readonly string OccurrenceCopyProtection = "ca_occurrences.copy_protection";
    public static readonly string OccurrenceCuratorialDescription = "ca_occurrences.description_curatorial";
    public static readonly string OccurrenceRelLeftId = "ca_occurrences_x_occurrences.occurrence_left_id";
}

public struct BundleNames
{
    public static readonly string PreferredLabels = "preferred_labels";
    public static readonly string DigitalObjectType = "digital_object_type";
    public static readonly string Format = "format";
    public static readonly string Quality = "quality";
    public static readonly string File = "file";
    public static readonly string FileName = "file_name";
    public static readonly string FileSize = "file_size";
    public static readonly string FedoraUrl = "fedora_url";
    public static readonly string WebsiteUrl = "website_url";
    public static readonly string InternalNotes = "internal_notes";
}

public struct Tables
{
    public static readonly string Occurrences = "ca_occurrences";
    public static readonly string OccurrencesXOccurrences = "ca_occurrences_x_occurrences";
    public static readonly string Objects = "ca_objects";
}

public struct Types
{
    public static readonly string Work = "work";
    public static readonly string WorkVersion = "work_version";
    public static readonly string DigitalObject = "digital_object";
    public static readonly string PhysicalObject = "physical_object";
    public static readonly string Paratext = "paratext";
}

public struct RelationTypes
{
    public static readonly string WorkManifestWorkVersion = "work_manifestation_workversion";
    public static readonly string WorkVersionRepresentParatext = "represented_by";
    public static readonly string WorkVersionContextualizeParatext = "contextualized_by";
    public static readonly string ParatextManifestPhysicalObject = "manifestation_of";
}

public struct SubjectRelationshipTypes
{
    public static readonly string ManifestationOf = "manifestation_of";
}

public struct Locales
{
    public static readonly string Czech = "cs_CZ";
    public static readonly string English = "en_US";
}
