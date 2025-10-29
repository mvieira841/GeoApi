using System;
using Asp.Versioning;

namespace GeoApi.Host.ApiHelpers;

public static class ApiVersionSet
{
    public static ApiVersion CurrentVersion { get; } = new(1.0);

    // Returns "v1", "v2", etc., derived from CurrentVersion
    public static string CurrentVersionString { get; } = BuildVersionTag(CurrentVersion);

    private static string BuildVersionTag(ApiVersion apiVersion)
    {
        if (apiVersion is null) return "v1";

        // ApiVersion.ToString() yields "1.0" (or similar). Take the major part.
        var text = apiVersion.ToString();
        var major = text.Split('.')[0].Trim();

        if (string.IsNullOrEmpty(major)) return "v1";

        return major.StartsWith("v", StringComparison.OrdinalIgnoreCase)
            ? major
            : $"v{major}";
    }
}