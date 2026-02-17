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

public struct Tables
{
    public static readonly string Occurrences = "ca_occurrences";
    public static readonly string OccurrencesXOccurrences = "ca_occurrences_x_occurrences";
}

public struct Types
{
    public static readonly string Work = "work";
    public static readonly string WorkVersion = "work_version";
}

public struct RelationTypes
{
    public static readonly string WorkManifestWorkVersion = "work_manifestation_workversion";
}

