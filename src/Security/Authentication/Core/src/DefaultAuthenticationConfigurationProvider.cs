// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Configuration;

namespace Microsoft.AspNetCore.Authentication;

internal sealed class DefaultAuthenticationConfigurationProvider : IAuthenticationConfigurationProvider
{
    public IConfigurationRoot Configuration { get;  }

    public DefaultAuthenticationConfigurationProvider(IConfiguration configuration)
    {
        if (configuration is not IConfigurationRoot configurationRoot)
        {
            throw new ArgumentException("Could not resolve IConfigurationRoot instance.");
        }
        Configuration = configurationRoot;
    }

    public IConfiguration GetSection(string name)
    {
        return Configuration.GetSection($"Authentication:Schemes:{name}");
    }
}
