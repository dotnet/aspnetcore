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
            return CsrfProtectionResult.Allowed();
        }

        // Step 2: Sec-Fetch-Site accept path. The vast majority of legitimate browser
        // traffic to a same-origin endpoint hits this exit without any DI lookup or string parsing.
        // https://developer.mozilla.org/en-US/docs/Web/HTTP/Reference/Headers/Sec-Fetch-Site#same-site
        var secFetchSite = request.Headers["Sec-Fetch-Site"].ToString();
        if (secFetchSite is "same-origin" or "none")
        {
            return CsrfProtectionResult.Allowed();
        }

        // Step 3: Check trusted origins from the CORS policy that applies to this request
        // (per-endpoint policy first, falling back to the default policy).
        var origin = request.Headers.Origin.ToString();
        if (!string.IsNullOrEmpty(origin))
        {
            var policy = await ResolveCorsPolicyAsync(context);

            // AllowAnyOrigin is intentionally NOT honored as a CSRF trust signal: it means "any browser
            // can read this resource", which is a different concern than "any origin can mutate it on the
            // user's behalf". Treating AllowAnyOrigin as trusted would turn this middleware into a no-op
            // for cross-origin writes. Users who legitimately need public-read CORS + CSRF-protected writes
            // should rely on Sec-Fetch-Site (modern browsers) / Origin-vs-Host (legacy) — which still apply
            // here — or opt the endpoint out via DisableAntiforgery() if it has no cookie-based auth.
            if (policy is not null && !policy.AllowAnyOrigin && policy.IsOriginAllowed(origin))
            {
                return CsrfProtectionResult.Allowed();
            }
        }

        // Step 4: Sec-Fetch-Site deny path. Any value other than "same-origin"/"none"
        // (e.g. "cross-site", "same-site") is treated as untrusted.
        if (!string.IsNullOrEmpty(secFetchSite))
        {
            return CsrfProtectionResult.Denied();
        }

        // Step 5: No Sec-Fetch-Site header. Fall back to Origin vs Host comparison.
        if (!string.IsNullOrEmpty(origin))
        {
            return OriginMatchesRequestHost(origin, request)
                ? CsrfProtectionResult.Allowed()
                : CsrfProtectionResult.Denied();
        }

        // Step 6: No Sec-Fetch-Site AND no Origin header.
        // This is a non-browser client (curl, Postman, server-to-server).
        // Allow the request — CSRF is a browser-based attack vector.
        return CsrfProtectionResult.Allowed();
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

    /// <summary>
    /// Compares the Origin header to "scheme://host[:port]" built from the request.
    /// </summary>
    /// <param name="origin">value of Origin header</param>
    /// <param name="request">request being processed</param>
    /// <remarks>
    /// This path only runs for pre-Fetch-Metadata browsers (~2020 and earlier) and
    /// non-browser clients, so the current allocation cost is negligible.
    /// </remarks>
    /// <returns>true if match</returns>
    private static bool OriginMatchesRequestHost(string origin, HttpRequest request)
    {
        var host = request.Host;
        if (!host.HasValue)
        {
            return false;
        }

        // host.Value preserves the raw "host[:port]" form as parsed from the Host header,
        // matching how browsers serialize the Origin header (default ports stripped on both sides).
        return MemoryExtensions.Equals(origin, $"{request.Scheme}://{host.Value}", StringComparison.OrdinalIgnoreCase);
    }
}
