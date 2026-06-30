using asec.Compatibility.CollectiveAccess.Models;

namespace asec.Compatibility.CollectiveAccess;

public static class IListExtensions
{
    public static string GetOptionalBundleValue(this IList<Bundle> bundles, string bundleCode)
    {
        return bundles?.FirstOrDefault(b => b.Code == bundleCode)?.Values?.FirstOrDefault()?.Value;
    }

    public static int? GetOptionalBundleIntValue(this IList<Bundle> bundles, string bundleCode)
        => int.TryParse(bundles.GetOptionalBundleValue(bundleCode), out var result) ? result : null;

    public static uint? GetOptionalBundleUintValue(this IList<Bundle> bundles, string bundleCode)
        => uint.TryParse(bundles.GetOptionalBundleValue(bundleCode), out var result) ? result : null;

    public static DateOnly? GetOptionalBundleDateValue(this IList<Bundle> bundles, string bundleCode)
        => DateOnly.TryParse(bundles.GetOptionalBundleValue(bundleCode), out var result) ? result : null;
}
