// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Http;

/// <summary>
/// Validates cross-origin requests using Fetch Metadata headers (Sec-Fetch-Site)
/// and Origin header as a fallback. This is a lightweight, synchronous check
/// that does not require DataProtection or token-based antiforgery services.
/// </summary>
internal static class CrossOriginRequestValidator
{
    private const string SecFetchSiteHeaderName = "Sec-Fetch-Site";
    private const string SecFetchSiteSameOrigin = "same-origin";
    private const string SecFetchSiteNone = "none";

    /// <summary>
    /// Validates whether the request should be allowed based on cross-origin headers.
    /// </summary>
    /// <returns>
    /// <c>true</c> if the request is allowed (same-origin, trusted, safe method, or non-browser);
    /// <c>false</c> if the request is a suspected cross-origin attack.
    /// </returns>
    public static bool IsRequestAllowed(HttpContext context, IList<string> trustedOrigins)
    {
        var request = context.Request;

        // Safe methods (GET, HEAD, OPTIONS, TRACE) never change state — always allow.
        if (HttpMethods.IsGet(request.Method) ||
            HttpMethods.IsHead(request.Method) ||
            HttpMethods.IsOptions(request.Method) ||
            HttpMethods.IsTrace(request.Method))
        {
            return true;
        }

        var headers = request.Headers;

        var origin = headers.Origin.ToString();
        var hasOrigin = !string.IsNullOrEmpty(origin) &&
                        !string.Equals(origin, "null", StringComparison.OrdinalIgnoreCase);

        // Step 1: Check if Origin is in trusted origins list
        if (hasOrigin && IsTrustedOrigin(origin, trustedOrigins))
        {
            return true;
        }

        // Step 2: Check Sec-Fetch-Site header (modern browsers, available since March 2023)
        var secFetchSite = headers[SecFetchSiteHeaderName].ToString();
        if (!string.IsNullOrEmpty(secFetchSite))
        {
            // "same-origin" means the request is from the same origin
            // "none" means user-initiated navigation (bookmark, typed URL)
            if (string.Equals(secFetchSite, SecFetchSiteSameOrigin, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(secFetchSite, SecFetchSiteNone, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            // "cross-site", "same-site", or any other value — suspected cross-origin attack
            return false;
        }

        // Step 3: Fallback to Origin vs request origin comparison (older browsers)
        if (hasOrigin)
        {
            return OriginMatchesRequestOrigin(origin, request);
        }

        // Step 4: Neither Sec-Fetch-Site nor Origin is present.
        // This is a non-browser client (curl, Postman, API client).
        // Non-browser clients are not susceptible to CSRF.
        return true;
    }

    private static bool IsTrustedOrigin(string origin, IList<string> trustedOrigins)
    {
        for (var i = 0; i < trustedOrigins.Count; i++)
        {
            if (string.Equals(origin, trustedOrigins[i], StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    private static bool OriginMatchesRequestOrigin(string origin, HttpRequest request)
    {
        if (!Uri.TryCreate(origin, UriKind.Absolute, out var originUri))
        {
            return false;
        }

        var requestHost = request.Host;
        if (!requestHost.HasValue)
        {
            return false;
        }

        // Scheme must match (http://example.com ≠ https://example.com)
        if (!string.Equals(originUri.Scheme, request.Scheme, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var originHost = originUri.IsDefaultPort ? originUri.Host : $"{originUri.Host}:{originUri.Port}";
        return string.Equals(originHost, requestHost.Value, StringComparison.OrdinalIgnoreCase);
    }
}
