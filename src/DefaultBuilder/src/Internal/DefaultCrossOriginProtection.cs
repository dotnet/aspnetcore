// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

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
            var requestOrigin = GetRequestOrigin(request);
            if (requestOrigin is not null && string.Equals(origin, requestOrigin, StringComparison.OrdinalIgnoreCase))
            {
                return CsrfProtectionResult.Allowed;
            }

            // Origin header is present but doesn't match the request's own origin → deny (fail closed).
            return CsrfProtectionResult.Denied;
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
