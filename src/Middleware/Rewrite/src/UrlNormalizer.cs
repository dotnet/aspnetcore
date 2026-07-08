// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Rewrite;

internal static class UrlNormalizer
{
    // Collapses a leading run of '/' and '\' to a single '/' so a redirect/rewrite target cannot resolve as a
    // scheme-relative authority. Mirrors the rejection predicate in SharedUrlHelper.IsLocalUrl.
    public static string CollapseLeadingSlashes(string url)
    {
        if (url.Length < 2 || url[0] != '/' || (url[1] != '/' && url[1] != '\\'))
        {
            return url;
        }

        var i = 1;
        while (i < url.Length && (url[i] == '/' || url[i] == '\\'))
        {
            i++;
        }

        return i == url.Length ? "/" : string.Concat("/", url.AsSpan(i));
    }
}
