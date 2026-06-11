// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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
            // Override: path-scoped to DBSC endpoints, match source lifetime
            options.Cookie.Path = "/.well-known/dbsc";
            options.SlidingExpiration = false;
        }
        else if (schemes.SessionSchemes.TryGetValue(name, out var sessionSourceScheme))
        {
            CopyFromSource(sessionSourceScheme, options);
            // Session cookie uses the source path (default /) but shorter expiry.
            // ExpireTimeSpan is set at sign-in time by the handler via AuthenticationProperties.
            options.SlidingExpiration = false;
        }
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

        // Restore the target-specific name and path
        target.Cookie.Name = targetName;
        if (targetPath is not null)
        {
            target.Cookie.Path = targetPath;
        }
    }
}
