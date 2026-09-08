// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Authentication.DeviceBoundSessions;

/// <summary>
/// If an application's default authenticate scheme resolves to a DBSC source scheme, redirects it to
/// the corresponding DBSC policy scheme. This keeps the user authenticated after registration swaps the
/// long-lived source cookie for the short-lived session cookie: the source cookie the app defaulted to
/// no longer exists, but the policy scheme reads the session cookie (falling back to the source cookie
/// before registration). Defaults the app pointed at any other scheme are left untouched, and the
/// sign-in/sign-out defaults are never changed.
/// </summary>
internal sealed class PostConfigureDeviceBoundSessionAuthenticationOptions : IPostConfigureOptions<AuthenticationOptions>
{
    private readonly IOptions<DeviceBoundSessionSourceSchemes> _sourceSchemes;

    public PostConfigureDeviceBoundSessionAuthenticationOptions(IOptions<DeviceBoundSessionSourceSchemes> sourceSchemes)
    {
        _sourceSchemes = sourceSchemes;
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
        if (_sourceSchemes.Value.PolicySchemes.TryGetValue(effectiveAuthenticateScheme, out var policyScheme))
        {
            options.DefaultAuthenticateScheme = policyScheme;
        }
    }
}
