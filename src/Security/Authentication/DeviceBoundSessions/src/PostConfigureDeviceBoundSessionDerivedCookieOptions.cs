// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma warning disable ASP0030 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

using Microsoft.AspNetCore.Authentication.Cookies;
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
            // Override: scope the refresh cookie to the DBSC endpoints' directory, derived from the
            // configured RefreshPath so a customized RefreshPath still receives the cookie. Lifetime and
            // sliding behavior are inherited from the source scheme so the refresh cookie ages exactly
            // like the auth cookie it replaces (renewed on each refresh when the source uses sliding).
            options.Cookie.Path = ResolveRefreshCookiePath(refreshSourceScheme);
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
        var targetName = target.Cookie.Name;
        var targetPath = target.Cookie.Path;

        target.Cookie.HttpOnly = source.Cookie.HttpOnly;
        target.Cookie.SecurePolicy = source.Cookie.SecurePolicy;
        target.Cookie.SameSite = source.Cookie.SameSite;
        target.Cookie.Domain = source.Cookie.Domain;
        target.Cookie.IsEssential = source.Cookie.IsEssential;
        target.ExpireTimeSpan = source.ExpireTimeSpan;
        target.SlidingExpiration = source.SlidingExpiration;

        // Restore the target-specific name and path
        target.Cookie.Name = targetName;
        if (targetPath is not null)
        {
            target.Cookie.Path = targetPath;
        }
    }
}
