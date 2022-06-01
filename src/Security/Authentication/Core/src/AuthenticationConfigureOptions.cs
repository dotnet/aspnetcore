// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Authentication;

internal sealed class AuthenticationConfigureOptions : IConfigureOptions<AuthenticationOptions>
{
    private readonly IAuthenticationConfigurationProvider _authenticationConfigurationProvider;

    public AuthenticationConfigureOptions(IAuthenticationConfigurationProvider configurationProvider)
    {
        _authenticationConfigurationProvider = configurationProvider;
    }

    public void Configure(AuthenticationOptions options)
    {
        var authenticationConfig = _authenticationConfigurationProvider.GetAuthenticationConfiguration();
        var defaultScheme = authenticationConfig[AuthenticationConfigurationConstants.DefaultScheme];
        if (defaultScheme is not null)
        {
            options.DefaultScheme = defaultScheme;
        }
    }
}
