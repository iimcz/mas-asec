namespace asec.Compatibility.CollectiveAccess.Models;


public record Relationship(
    int Id,
    IList<Bundle> Bundles
);

public record SubjectRelationship(
    string RelationshipType,
    string Target,
    int TargetId
);
