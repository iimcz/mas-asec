namespace asec.Compatibility.CollectiveAccess.Models;

public record Work(
    int Id,
    string Idno,
    IList<Bundle> Bundles
);

public record WorkVersion(
    int Id,
    string Idno,
    IList<Bundle> Bundles
);

public record Paratext(
    int Id,
    string Idno,
    IList<Bundle> Bundles
);

public record DigitalObject(
    int Id,
    string Idno,
    IList<Bundle> Bundles
);

public record PhysicalObject(
    int Id,
    string Idno,
    IList<Bundle> Bundles
);
