namespace asec.Compatibility.CollectiveAccess.Models;

public record Work(
    int Id,
    string Idno,
    IList<Bundle> Bundles
);

public record Relationship(
    int Id,
    IList<Bundle> Bundles
);

public record WorkVersion(
    int Id,
    string Idno,
    IList<Bundle> Bundles
);
