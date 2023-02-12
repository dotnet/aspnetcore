// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Configuration;

namespace Microsoft.AspNetCore.Authentication;

internal sealed class DefaultAuthenticationConfigurationProvider : IAuthenticationConfigurationProvider
{
    private readonly IConfiguration _configuration;
    private const string AuthenticationKey = "Authentication";

    // Note: this generally will never be called except in unit tests as IConfiguration is generally available from the host
    public DefaultAuthenticationConfigurationProvider() : this(new ConfigurationManager())
    { }

    public DefaultAuthenticationConfigurationProvider(IConfiguration configuration)
        => _configuration = configuration;

    public IConfiguration AuthenticationConfiguration => _configuration.GetSection(AuthenticationKey);
}
