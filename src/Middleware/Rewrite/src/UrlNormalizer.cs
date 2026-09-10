// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Rewrite;

internal static class UrlNormalizer
{
    // Collapses a leading run of '/' and '\' to a single '/' so a redirect/rewrite target cannot resolve as a
    // scheme-relative authority. Mirrors the rejection predicate in SharedUrlHelper.IsLocalUrl.
    public static string CollapseLeadingSlashes(string url)
    {
        if (string.IsNullOrEmpty(url))
        {
            return url;
        }

        if ((url[0] != '/' && url[0] != '\\') ||
            (url[0] == '/' && (url.Length == 1 || (url[1] != '/' && url[1] != '\\'))))
        {
            return url;
        }

        var firstNonSlash = url.AsSpan().IndexOfAnyExcept('/', '\\');

        if (firstNonSlash < 0)
        {
            return "/";
        }

        return string.Concat("/", url.AsSpan(firstNonSlash));
    }
}
