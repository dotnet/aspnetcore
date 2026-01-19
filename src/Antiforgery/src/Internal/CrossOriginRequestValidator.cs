// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Antiforgery.CrossOrigin;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Antiforgery.Internal;

/// <summary>
/// Validates cross-origin requests using Fetch Metadata headers (Sec-Fetch-Site)
/// and Origin header as a fallback, following the algorithm described in
/// https://web.dev/articles/fetch-metadata and Go 1.25's CrossOriginProtection.
/// </summary>
internal sealed class CrossOriginRequestValidator : ICrossOriginAntiforgery
{
    // https://developer.mozilla.org/docs/Web/HTTP/Reference/Headers/Sec-Fetch-Site#same-site
    private const string SecFetchSiteHeaderName = "Sec-Fetch-Site";
    private const string SecFetchSiteSameOrigin = "same-origin";
    private const string SecFetchSiteNone = "none";
    private const string SecFetchSiteCrossSite = "cross-site";
    private const string SecFetchSiteSameSite = "same-site";

    private readonly CrossOriginAntiforgeryOptions _options;

    public CrossOriginRequestValidator(IOptions<CrossOriginAntiforgeryOptions> options)
    {
        _options = options.Value;
    }

    /// <summary>
    /// Validates the incoming HTTP request against cross-origin antiforgery requirements.
    /// </summary>
    /// <remarks>
    /// The algorithm follows these steps:
    /// 1. If Origin header matches a trusted origin, allow the request.
    /// 2. If Sec-Fetch-Site header is present:
    ///    - "same-origin" or "none" → allow
    ///    - "cross-site" or "same-site" → deny
    /// 3. If Origin header is present (but not Sec-Fetch-Site):
    ///    - Compare Origin's host with Host header
    ///    - Match → allow, No match → deny
    /// 4. If neither Sec-Fetch-Site nor Origin is present:
    ///    - Return Unknown (fallback to token-based validation)
    /// </remarks>
    public CrossOriginValidationResult Validate(HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var request = context.Request;
        var headers = request.Headers;

        // Get the Origin header value
        var origin = headers.Origin.ToString();
        var hasOrigin = !string.IsNullOrEmpty(origin) && !string.Equals(origin, "null", StringComparison.OrdinalIgnoreCase);

        // Step 1: Check if Origin is in trusted origins list
        if (hasOrigin && IsTrustedOrigin(origin))
        {
            return CrossOriginValidationResult.Allowed;
        }

        // Step 2: Check Sec-Fetch-Site header (modern browsers)
        var secFetchSite = headers[SecFetchSiteHeaderName].ToString();
        if (!string.IsNullOrEmpty(secFetchSite))
        {
            // "same-origin" means the request is from the same origin
            // "none" means the request was user-initiated (e.g., bookmark, typed URL)
            if (string.Equals(secFetchSite, SecFetchSiteSameOrigin, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(secFetchSite, SecFetchSiteNone, StringComparison.OrdinalIgnoreCase))
            {
                return CrossOriginValidationResult.Allowed;
            }

            // "cross-site" or "same-site" means the request is from a different origin/site
            // This is a potential CSRF attack
            if (string.Equals(secFetchSite, SecFetchSiteCrossSite, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(secFetchSite, SecFetchSiteSameSite, StringComparison.OrdinalIgnoreCase))
            {
                return CrossOriginValidationResult.Denied;
            }

            // Unknown Sec-Fetch-Site value - treat as suspicious
            return CrossOriginValidationResult.Denied;
        }

        // Step 3: Fallback to Origin vs Host comparison (older browsers)
        if (hasOrigin)
        {
            if (OriginMatchesHost(origin, headers.Host.ToString()))
            {
                return CrossOriginValidationResult.Allowed;
            }

            // Origin doesn't match Host - this is a cross-origin request
            return CrossOriginValidationResult.Denied;
        }

        // Step 4: Neither Sec-Fetch-Site nor Origin is present
        // This could be:
        // - A non-browser request (curl, Postman, API client)
        // - A very old browser
        // - A privacy extension that strips headers
        // Return Unknown to trigger fallback to token-based validation
        return CrossOriginValidationResult.Unknown;
    }

    /// <summary>
    /// Checks if the origin is in the trusted origins list.
    /// </summary>
    private bool IsTrustedOrigin(string origin)
    {
        foreach (var trustedOrigin in _options.TrustedOrigins)
        {
            if (string.Equals(origin, trustedOrigin, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Compares the host portion of the Origin header with the Host header.
    /// </summary>
    /// <remarks>
    /// The Origin header format is: scheme://host[:port]
    /// The Host header format is: host[:port]
    /// We extract the host (and port if present) from Origin and compare with Host.
    /// </remarks>
    private static bool OriginMatchesHost(string origin, string host)
    {
        if (string.IsNullOrEmpty(host))
        {
            return false;
        }

        // Parse the Origin to extract host and port
        if (!Uri.TryCreate(origin, UriKind.Absolute, out var originUri))
        {
            return false;
        }

        // Build the host:port string from the Origin URI
        var originHost = originUri.IsDefaultPort
            ? originUri.Host
            : $"{originUri.Host}:{originUri.Port}";

        // Compare with the Host header (case-insensitive)
        return string.Equals(originHost, host, StringComparison.OrdinalIgnoreCase);
    }
}
