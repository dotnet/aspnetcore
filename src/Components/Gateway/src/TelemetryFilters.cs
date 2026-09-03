// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components.Gateway;

internal static class TelemetryFilters
{
    public static bool ShouldTraceInboundRequest(PathString requestPath, IList<string> excludePaths)
    {
        if (!requestPath.HasValue)
        {
            return true;
        }

        foreach (var excluded in excludePaths)
        {
            if (requestPath.StartsWithSegments(NormalizeExcludeSegment(excluded)))
            {
                return false;
            }
        }

        return true;
    }

    public static bool ShouldTraceOutboundRequest(Uri? requestUri, IList<string> excludeOutboundPaths)
    {
        if (requestUri is null)
        {
            return true;
        }

        foreach (var excluded in excludeOutboundPaths)
        {
            if (requestUri.AbsolutePath.StartsWith(excluded, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }
        }

        return true;
    }

    private static PathString NormalizeExcludeSegment(string excluded)
    {
        if (string.IsNullOrEmpty(excluded))
        {
            return PathString.Empty;
        }

        if (excluded.Length > 1 && excluded[^1] == '/')
        {
            return new PathString(excluded[..^1]);
        }

        return new PathString(excluded);
    }
}
