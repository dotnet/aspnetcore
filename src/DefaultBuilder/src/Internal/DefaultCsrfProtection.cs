// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Antiforgery;

internal sealed class DefaultCsrfProtection : ICsrfProtection
{
    // Safe HTTP methods that do not require cross-origin validation (RFC 7231).
    private static readonly HashSet<string> SafeMethods = new(StringComparer.OrdinalIgnoreCase)
    {
        HttpMethods.Get,
        HttpMethods.Head,
        HttpMethods.Options,
        HttpMethods.Trace,
    };

    /// <inheritdoc />
    public async ValueTask<CsrfProtectionResult> ValidateAsync(HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var request = context.Request;

        // Step 1: Safe methods are always allowed.
        if (SafeMethods.Contains(request.Method))
        {
            return CsrfProtectionResult.Allowed;
        }

        // Step 2: Check trusted origins from the CORS policy that applies to this request
        // (per-endpoint policy first, falling back to the default policy).
        var origin = request.Headers.Origin.ToString();
        if (!string.IsNullOrEmpty(origin))
        {
            var policy = await ResolveCorsPolicyAsync(context);
            if (policy is not null && !policy.AllowAnyOrigin && policy.IsOriginAllowed(origin))
            {
                return CsrfProtectionResult.Allowed;
            }
        }

        // Step 3: Sec-Fetch-Site header (set by browsers per Fetch Metadata spec).
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
            return OriginMatchesRequestHost(origin, request)
                ? CsrfProtectionResult.Allowed
                : CsrfProtectionResult.Denied;
        }

        // Step 5: No Sec-Fetch-Site AND no Origin header.
        // This is a non-browser client (curl, Postman, server-to-server).
        // Allow the request — CSRF is a browser-based attack vector.
        return CsrfProtectionResult.Allowed;
    }

    private static async ValueTask<CorsPolicy?> ResolveCorsPolicyAsync(HttpContext context)
    {
        var corsMetadata = context.GetEndpoint()?.Metadata.GetMetadata<ICorsMetadata>();

        // [DisableCors] on the endpoint → no CORS-derived trust applies. Fall through to Sec-Fetch logic.
        if (corsMetadata is IDisableCorsAttribute)
        {
            return null;
        }

        // Inline policy attached to the endpoint (rare, but supported by the CORS metadata model).
        if (corsMetadata is ICorsPolicyMetadata inlinePolicyMetadata)
        {
            return inlinePolicyMetadata.Policy;
        }

        // [EnableCors("name")] selects a named policy; otherwise null → ICorsPolicyProvider falls back
        // to the default policy registered via AddCors(o => o.AddDefaultPolicy(...)).
        var policyName = (corsMetadata as IEnableCorsAttribute)?.PolicyName;
        var provider = context.RequestServices.GetService<ICorsPolicyProvider>();
        if (provider is null)
        {
            return null;
        }

        return await provider.GetPolicyAsync(context, policyName);
    }

    // Parses an Origin header value ("scheme://host[:port]") and compares each component
    // against the request's scheme/host/port without allocating any temporary strings.
    // Handles IPv6 literals ("[::1]:8080") and treats omitted ports as the scheme's default.
    private static bool OriginMatchesRequestHost(string origin, HttpRequest request)
    {
        var host = request.Host;
        if (!host.HasValue)
        {
            return false;
        }

        // Locate "://"
        var schemeEnd = origin.IndexOf("://", StringComparison.Ordinal);
        if (schemeEnd <= 0)
        {
            return false;
        }

        // Compare scheme.
        var scheme = request.Scheme;
        if (!origin.AsSpan(0, schemeEnd).Equals(scheme, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var hostAndPort = origin.AsSpan(schemeEnd + 3);
        if (hostAndPort.IsEmpty)
        {
            return false;
        }

        ReadOnlySpan<char> originHostSpan;
        int originPort;

        if (hostAndPort[0] == '[')
        {
            // IPv6 literal: "[host]:port" or "[host]". HostString.Host preserves the brackets,
            // so we keep them in the origin span as well to compare apples to apples.
            var bracketEnd = hostAndPort.IndexOf(']');
            if (bracketEnd < 2)
            {
                return false;
            }

            originHostSpan = hostAndPort.Slice(0, bracketEnd + 1);

            if (bracketEnd + 1 == hostAndPort.Length)
            {
                originPort = -1;
            }
            else if (hostAndPort[bracketEnd + 1] == ':')
            {
                if (!int.TryParse(hostAndPort.Slice(bracketEnd + 2), out originPort) || originPort < 0)
                {
                    return false;
                }
            }
            else
            {
                // Anything other than ':' or end-of-input after ']' is malformed.
                return false;
            }
        }
        else
        {
            var portColon = hostAndPort.IndexOf(':');
            if (portColon < 0)
            {
                originHostSpan = hostAndPort;
                originPort = -1;
            }
            else
            {
                originHostSpan = hostAndPort.Slice(0, portColon);
                if (!int.TryParse(hostAndPort.Slice(portColon + 1), out originPort) || originPort < 0)
                {
                    return false;
                }
            }
        }

        // Compare host (HostString.Host preserves brackets for IPv6 literals — match that here).
        if (!originHostSpan.Equals(host.Host, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        // Compare ports, treating an omitted port as the scheme's default.
        var requestPort = host.Port ?? DefaultPortForScheme(scheme);
        var normalizedOriginPort = originPort >= 0 ? originPort : DefaultPortForScheme(scheme);
        return requestPort == normalizedOriginPort;
    }

    private static int DefaultPortForScheme(string scheme)
    {
        if (string.Equals(scheme, "https", StringComparison.OrdinalIgnoreCase))
        {
            return 443;
        }

        if (string.Equals(scheme, "http", StringComparison.OrdinalIgnoreCase))
        {
            return 80;
        }

        // Unknown scheme: no default. Caller compares with the explicit value (if any),
        // which will only match when both sides agree explicitly.
        return -1;
    }
}
