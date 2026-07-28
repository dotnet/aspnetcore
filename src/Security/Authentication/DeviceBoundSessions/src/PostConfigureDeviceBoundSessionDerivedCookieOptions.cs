// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma warning disable ASP0031 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Authentication.DeviceBoundSessions;

/// <summary>
/// Copies cookie settings from the source scheme to the refresh and session cookie schemes
/// so they match HttpOnly, Secure, SameSite, Domain, and lifetime settings.
/// </summary>
internal sealed class PostConfigureDeviceBoundSessionDerivedCookieOptions : IPostConfigureOptions<CookieAuthenticationOptions>
{
    private readonly IOptions<DeviceBoundSessionSourceSchemes> _sourceSchemes;
    private readonly IServiceProvider _services;

    public PostConfigureDeviceBoundSessionDerivedCookieOptions(
        IOptions<DeviceBoundSessionSourceSchemes> sourceSchemes,
        IServiceProvider services)
    {
        _sourceSchemes = sourceSchemes;
        _services = services;
    }

    public void PostConfigure(string? name, CookieAuthenticationOptions options)
    {
        ArgumentNullException.ThrowIfNull(name);

        var schemes = _sourceSchemes.Value;

        if (schemes.RefreshSchemes.TryGetValue(name, out var refreshSourceScheme))
        {
            CopyFromSource(refreshSourceScheme, options);
            // Scope the refresh cookie to the DBSC endpoints' directory, applied relative to the request
            // path base so an app mounted under a non-root path base still receives the cookie at its
            // advertised refresh endpoint. Lifetime and sliding behavior are inherited from the source
            // scheme so the refresh cookie ages exactly like the auth cookie it replaces.
            options.Cookie = CreatePathBaseScopedCookie(options.Cookie, ResolveRefreshCookiePath(refreshSourceScheme));
            // Re-run the source scheme's principal validation (e.g. Identity's security-stamp / revocation
            // check) on the long-lived refresh cookie so revocation is enforced at refresh time — which the
            // short-lived session cookie would otherwise bypass.
            ForwardSourceValidatePrincipal(options, refreshSourceScheme);
        }
        else if (schemes.SessionSchemes.TryGetValue(name, out var sessionSourceScheme))
        {
            CopyFromSource(sessionSourceScheme, options);
            // Session cookie uses the source path (default /) but shorter expiry.
            // ExpireTimeSpan is set at sign-in time by the handler via AuthenticationProperties.
            options.SlidingExpiration = false;
        }
    }

    private string ResolveRefreshCookiePath(string sourceScheme)
    {
        const string fallback = "/.well-known/dbsc";

        // Map source cookie scheme -> DBSC handler scheme so we can read its configured RefreshPath.
        if (!_sourceSchemes.Value.Schemes.TryGetValue(sourceScheme, out var dbscScheme))
        {
            return fallback;
        }

        var refreshPath = _services.GetRequiredService<IOptionsMonitor<DeviceBoundSessionOptions>>()
            .Get(dbscScheme).RefreshPath.Value;
        if (string.IsNullOrEmpty(refreshPath))
        {
            return fallback;
        }

        // Scope the cookie to the refresh endpoint's directory so it is sent to that endpoint.
        var lastSlash = refreshPath.LastIndexOf('/');
        return lastSlash > 0 ? refreshPath[..lastSlash] : "/";
    }

    private void CopyFromSource(string sourceScheme, CookieAuthenticationOptions target)
    {
        var source = _services.GetRequiredService<IOptionsMonitor<CookieAuthenticationOptions>>().Get(sourceScheme);

        // Copy cookie builder settings (preserving any name/path already set)
        target.Cookie.HttpOnly = source.Cookie.HttpOnly;
        target.Cookie.SecurePolicy = source.Cookie.SecurePolicy;
        target.Cookie.SameSite = source.Cookie.SameSite;
        target.Cookie.Domain = source.Cookie.Domain;
        target.Cookie.IsEssential = source.Cookie.IsEssential;
        target.ExpireTimeSpan = source.ExpireTimeSpan;
        target.SlidingExpiration = source.SlidingExpiration;
    }

    // Forwards the source scheme's OnValidatePrincipal to the refresh cookie so revocation checks
    // (e.g. Identity's security stamp) run when the long-lived refresh cookie is used. Resolves the
    // source's events at request time to support both delegate-based Events and EventsType, and invokes
    // only ValidatePrincipal — not the source's sign-in/sign-out handlers.
    private static void ForwardSourceValidatePrincipal(CookieAuthenticationOptions options, string sourceScheme)
    {
        var priorValidatePrincipal = options.Events.OnValidatePrincipal;
        options.Events.OnValidatePrincipal = async context =>
        {
            var services = context.HttpContext.RequestServices;
            var sourceOptions = services.GetRequiredService<IOptionsMonitor<CookieAuthenticationOptions>>().Get(sourceScheme);
            var sourceEvents = sourceOptions.EventsType is not null
                ? (CookieAuthenticationEvents)services.GetRequiredService(sourceOptions.EventsType)
                : sourceOptions.Events;

            await sourceEvents.ValidatePrincipal(context);
            await priorValidatePrincipal(context);
        };
    }

    // Rebuilds the cookie as a path-base-aware builder so the cookie path becomes
    // "{request path base}{additionalPath}" at emit time, instead of a fixed absolute path.
    private static CookieBuilder CreatePathBaseScopedCookie(CookieBuilder source, string additionalPath)
        => new PathBaseScopedCookieBuilder(additionalPath)
        {
            Name = source.Name,
            HttpOnly = source.HttpOnly,
            SecurePolicy = source.SecurePolicy,
            SameSite = source.SameSite,
            Domain = source.Domain,
            IsEssential = source.IsEssential,
            MaxAge = source.MaxAge,
            Expiration = source.Expiration,
            // Path is intentionally left unset so the builder derives (path base + additional path) per request.
        };

    private sealed class PathBaseScopedCookieBuilder : RequestPathBaseCookieBuilder
    {
        private readonly string _additionalPath;

        public PathBaseScopedCookieBuilder(string additionalPath)
        {
            _additionalPath = additionalPath;
        }

        protected override string AdditionalPath => _additionalPath;
    }
}
