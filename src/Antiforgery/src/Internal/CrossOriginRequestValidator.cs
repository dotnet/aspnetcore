// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Antiforgery.CrossOrigin;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Antiforgery.Internal;

/// <summary>
/// Validates cross-origin requests using Fetch Metadata headers (Sec-Fetch-Site)
/// and Origin header as a fallback.
/// </summary>
internal sealed class CrossOriginRequestValidator : ICrossOriginAntiforgery
{
    private const string SecFetchSiteHeaderName = "Sec-Fetch-Site";
    private const string SecFetchSiteSameOrigin = "same-origin";
    private const string SecFetchSiteNone = "none";

    private readonly CrossOriginAntiforgeryOptions _options;

    public CrossOriginRequestValidator(IOptions<CrossOriginAntiforgeryOptions> options)
    {
        _options = options.Value;
    }

    public CrossOriginValidationResult Validate(HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var request = context.Request;
        var headers = request.Headers;

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
            if (string.Equals(secFetchSite, SecFetchSiteSameOrigin, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(secFetchSite, SecFetchSiteNone, StringComparison.OrdinalIgnoreCase))
            {
                return CrossOriginValidationResult.Allowed;
            }

            return CrossOriginValidationResult.Denied;
        }

        // Step 3: Fallback to Origin vs Host comparison (older browsers)
        if (hasOrigin)
        {
            return OriginMatchesHost(origin, headers.Host.ToString())
                ? CrossOriginValidationResult.Allowed
                : CrossOriginValidationResult.Denied;
        }

        // Step 4: Neither Sec-Fetch-Site nor Origin is present
        return CrossOriginValidationResult.Unknown;
    }

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

    private static bool OriginMatchesHost(string origin, string host)
    {
        if (string.IsNullOrEmpty(host))
        {
            return false;
        }

        if (!Uri.TryCreate(origin, UriKind.Absolute, out var originUri))
        {
            return false;
        }

        var originHost = originUri.IsDefaultPort
            ? originUri.Host
            : $"{originUri.Host}:{originUri.Port}";

        return string.Equals(originHost, host, StringComparison.OrdinalIgnoreCase);
    }
}
