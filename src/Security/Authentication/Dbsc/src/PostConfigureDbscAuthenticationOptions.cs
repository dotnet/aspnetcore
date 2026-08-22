// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma warning disable ASP0031 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Authentication.Dbsc;

/// <summary>
/// If an application's default authenticate scheme resolves to a DBSC source scheme, redirects it to
/// the corresponding DBSC scheme, which authenticates against the session scheme and then the source scheme.
/// This keeps the user authenticated after registration swaps the
/// long-lived source cookie for the short-lived session cookie: the source cookie the app defaulted to
/// no longer exists, but the DBSC scheme reads the session cookie (falling back to the source cookie before
/// registration). Defaults the app pointed at any other scheme are left untouched, and the
/// sign-in/sign-out defaults are never changed.
/// </summary>
internal sealed class PostConfigureDbscAuthenticationOptions : IPostConfigureOptions<AuthenticationOptions>
{
    private readonly IOptions<DbscSourceSchemes> _sourceSchemes;
    private readonly IOptionsMonitor<DbscOptions> _dbscOptions;

    public PostConfigureDbscAuthenticationOptions(
        IOptions<DbscSourceSchemes> sourceSchemes,
        IOptionsMonitor<DbscOptions> dbscOptions)
    {
        _sourceSchemes = sourceSchemes;
        _dbscOptions = dbscOptions;
    }

    public void PostConfigure(string? name, AuthenticationOptions options)
    {
        // The effective authenticate scheme is DefaultAuthenticateScheme, falling back to DefaultScheme.
        var effectiveAuthenticateScheme = options.DefaultAuthenticateScheme ?? options.DefaultScheme;
        if (effectiveAuthenticateScheme is null)
        {
            return;
        }

        // Only upgrade when the app's default authenticate scheme is a scheme we wrapped with DBSC.
        var dbscScheme = _sourceSchemes.Value.FindDbscScheme(effectiveAuthenticateScheme, _dbscOptions);
        if (dbscScheme is not null)
        {
            options.DefaultAuthenticateScheme = dbscScheme;
        }
    }
}
