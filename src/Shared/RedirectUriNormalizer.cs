// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;

namespace Microsoft.AspNetCore.Authentication;

// Shared between Microsoft.AspNetCore.Authentication (RemoteAuthenticationHandler) and the remote handlers
// that own additional callback paths of their own (e.g. Microsoft.AspNetCore.Authentication.OpenIdConnect).
internal static class RedirectUriNormalizer
{
    // Collapses a leading run of '/' and '\' to a single '/' so a return URI derived from the request target
    // cannot resolve as a scheme-relative authority. Mirrors Rewrite's UrlNormalizer.CollapseLeadingSlashes.
    [return: NotNullIfNotNull(nameof(uri))]
    public static string? CollapseLeadingSlashes(string? uri)
    {
        if (string.IsNullOrEmpty(uri))
        {
            return uri;
        }

        if ((uri[0] != '/' && uri[0] != '\\') ||
            (uri[0] == '/' && (uri.Length == 1 || (uri[1] != '/' && uri[1] != '\\'))))
        {
            return uri;
        }

        var firstNonSlash = uri.AsSpan().IndexOfAnyExcept('/', '\\');

        if (firstNonSlash < 0)
        {
            return "/";
        }

        return string.Concat("/", uri.AsSpan(firstNonSlash));
    }
}
