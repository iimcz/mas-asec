using asec.Compatibility.CollectiveAccess.Models;

namespace asec.Compatibility.CollectiveAccess;

public static class IListExtensions
{
    public static string GetOptionalBundleValue(this IList<Bundle> bundles, string bundleCode)
    {
        return bundles?.FirstOrDefault(b => b.Code == bundleCode)?.Values?.FirstOrDefault()?.Value;
    }
}
