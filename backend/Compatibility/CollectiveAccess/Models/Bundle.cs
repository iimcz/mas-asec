namespace asec.Compatibility.CollectiveAccess.Models;

public record BundleValue(
    int? Id,
    string Locale,
    string Value
);

public record Bundle(
    string Code,
    string DataType,
    string Name,
    IList<BundleValue> Values
);

public record InputBundle(
    string Locale,
    string Name,
    string Value,
    bool Replace = true
);
