namespace asec.Compatibility.CollectiveAccess.Models;

public struct BundleCodes
{
    public static string OccurrenceLabel = "ca_occurrences.preferred_labels";
    public static string OccurrenceId = "ca_occurrences.occurrence_id";
    public static string OccurrenceRelLeftId = "ca_occurrences_x_occurrences.occurrence_left_id";
}

public struct Tables
{
    public static string Occurrences = "ca_occurrences";
    public static string OccurrencesXOccurrences = "ca_occurrences_x_occurrences";
}

public struct Types
{
    public static string Work = "work";
    public static string WorkVersion = "work_version";
}

public struct RelationTypes
{
    public static string WorkManifestWorkVersion = "work_manifestation_workversion";
}

