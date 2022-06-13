// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Authentication;

internal sealed class AuthenticationConfigureOptions : IConfigureOptions<AuthenticationOptions>
{
    private readonly IAuthenticationConfigurationProvider _authenticationConfigurationProvider;
    private const string DefaultSchemeKey = "DefaultScheme";

    public AuthenticationConfigureOptions(IAuthenticationConfigurationProvider configurationProvider)
    {
        _authenticationConfigurationProvider = configurationProvider;
    }

    public void Configure(AuthenticationOptions options)
    {
        var authenticationConfig = _authenticationConfigurationProvider.AuthenticationConfiguration;
        var defaultScheme = authenticationConfig[DefaultSchemeKey];
        // Only set the default scheme from config if it has not
        // already been set and is provided in options
        if (!string.IsNullOrEmpty(defaultScheme) && string.IsNullOrEmpty(options.DefaultScheme))
        {
            options.DefaultScheme = defaultScheme;
        }
    }
}
