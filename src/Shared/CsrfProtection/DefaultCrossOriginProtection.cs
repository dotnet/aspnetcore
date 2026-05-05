// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Antiforgery;

internal sealed class DefaultCrossOriginProtection : ICsrfProtection
{
    // Safe HTTP methods that do not require cross-origin validation (RFC 7231).
    private static readonly HashSet<string> SafeMethods = new(StringComparer.OrdinalIgnoreCase)
    {
        HttpMethods.Get,
        HttpMethods.Head,
        HttpMethods.Options,
        HttpMethods.Trace,
    };

    private readonly HashSet<string> _trustedOrigins;

    public DefaultCrossOriginProtection() : this([])
    {
    }

    public DefaultCrossOriginProtection(IEnumerable<string> trustedOrigins)
    {
        _trustedOrigins = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var origin in trustedOrigins)
        {
            if (TryNormalizeOrigin(origin, out var normalized))
            {
                _trustedOrigins.Add(normalized);
            }
        }
    }

    /// <inheritdoc />
    public CsrfProtectionResult Validate(HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var request = context.Request;

        // Step 1: Safe methods are always allowed.
        if (SafeMethods.Contains(request.Method))
        {
            return CsrfProtectionResult.Allowed;
        }

        // Step 2: Check trusted origins against the Origin header.
        var origin = request.Headers.Origin.ToString();
        if (!string.IsNullOrEmpty(origin) && _trustedOrigins.Count > 0)
        {
            if (TryNormalizeOrigin(origin, out var normalizedOrigin) && _trustedOrigins.Contains(normalizedOrigin))
            {
                return CsrfProtectionResult.Allowed;
            }
        }

        // Step 3: Sec-Fetch-Site header (set by browsers per Fetch Metadata spec)
        // https://developer.mozilla.org/en-US/docs/Web/HTTP/Reference/Headers/Sec-Fetch-Site#same-site
        var secFetchSite = request.Headers["Sec-Fetch-Site"].ToString();
        if (!string.IsNullOrEmpty(secFetchSite))
        {
            return secFetchSite switch
            {
                "same-origin" => CsrfProtectionResult.Allowed,
                "none" => CsrfProtectionResult.Allowed,
                _ => CsrfProtectionResult.Denied,
            };
        }

        // Step 4: No Sec-Fetch-Site header. Fall back to Origin vs Host comparison.
        if (!string.IsNullOrEmpty(origin))
        {
            var requestOrigin = GetRequestOrigin(request);
            if (requestOrigin is not null && TryNormalizeOrigin(origin, out var normalizedOrigin))
            {
                return string.Equals(normalizedOrigin, requestOrigin, StringComparison.OrdinalIgnoreCase)
                    ? CsrfProtectionResult.Allowed
                    : CsrfProtectionResult.Denied;
            }

            // Malformed Origin header → deny (fail closed).
            return CsrfProtectionResult.Denied;
        }

        // Step 5: No Sec-Fetch-Site AND no Origin header.
        // This is a non-browser client (curl, Postman, server-to-server).
        // Allow the request — CSRF is a browser-based attack vector.
        return CsrfProtectionResult.Allowed;
    }

    private static string? GetRequestOrigin(HttpRequest request)
    {
        var host = request.Host;
        if (!host.HasValue)
        {
            return null;
        }

        var scheme = request.Scheme;
        var port = host.Port;

        if (IsDefaultPort(scheme, port))
        {
            return $"{scheme}://{host.Host}";
        }

        return $"{scheme}://{host.Host}:{port}";
    }

    internal static bool TryNormalizeOrigin(string origin, [NotNullWhen(true)] out string? normalized)
    {
        normalized = null;

        if (string.IsNullOrWhiteSpace(origin))
        {
            return false;
        }

        if (!Uri.TryCreate(origin, UriKind.Absolute, out var uri))
        {
            return false;
        }

        // Origins must not have a path (beyond "/"), query, or fragment.
        if (uri.PathAndQuery != "/" || !string.IsNullOrEmpty(uri.Fragment))
        {
            return false;
        }

        var scheme = uri.Scheme;
        var host = uri.Host;
        var port = uri.Port;

        if (IsDefaultPort(scheme, port))
        {
            normalized = $"{scheme}://{host}";
        }
        else
        {
            normalized = $"{scheme}://{host}:{port}";
        }

        return true;
    }

    private static bool IsDefaultPort(string scheme, int? port)
    {
        if (port is null or -1)
        {
            return true;
        }

        return (scheme == "https" && port == 443) ||
               (scheme == "http" && port == 80);
    }
}
