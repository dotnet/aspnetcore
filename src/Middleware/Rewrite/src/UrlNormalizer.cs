// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Rewrite;

internal static class UrlNormalizer
{
    // RFC 3986 section 3.1: scheme = ALPHA *( ALPHA / DIGIT / "+" / "-" / "." ), matching Uri.CheckSchemeName without allocating a substring.
    private static readonly SearchValues<char> s_validSchemeChars =
        SearchValues.Create("+-.0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz");

    public static bool HasAbsoluteSchemeAndAuthority(string url, out int schemeSplit)
    {
        schemeSplit = url.IndexOf(Uri.SchemeDelimiter, StringComparison.Ordinal);
        if (schemeSplit <= 0 || !IsValidSchemeName(url.AsSpan(0, schemeSplit)))
        {
            return false;
        }

        var remainder = url.AsSpan(schemeSplit + Uri.SchemeDelimiter.Length);
        var delimiterIndex = remainder.IndexOfAny('/', '?', '#');
        var authority = delimiterIndex < 0 ? remainder : remainder.Slice(0, delimiterIndex);

        return !authority.IsEmpty;
    }

    private static bool IsValidSchemeName(ReadOnlySpan<char> scheme)
    {
        return !scheme.IsEmpty
            && char.IsAsciiLetter(scheme[0])
            && !scheme.Slice(1).ContainsAnyExcept(s_validSchemeChars);
    }

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

    public static PathString NormalizeRelativePath(string path)
    {
        if (string.IsNullOrEmpty(path))
        {
            return new PathString("/");
        }

        if (path[0] is not '/' and not '\\')
        {
            path = '/' + path;
        }

        var decoded = PathString.FromUriComponent(path);
        var collapsed = CollapseLeadingSlashes(decoded.Value!);

        return ReferenceEquals(collapsed, decoded.Value) ? decoded : new PathString(collapsed);
    }
}
