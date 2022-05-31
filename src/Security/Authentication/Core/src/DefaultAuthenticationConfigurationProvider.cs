// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Configuration;

namespace Microsoft.AspNetCore.Authentication;

internal sealed class DefaultAuthenticationConfigurationProvider : IAuthenticationConfigurationProvider
{
    private readonly IConfiguration _configuration;

    public DefaultAuthenticationConfigurationProvider(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public IConfiguration GetAuthenticationSchemeConfiguration(string authenticationScheme)
    {
        return _configuration.GetSection($"Authentication:Schemes:{authenticationScheme}");
    }
}
