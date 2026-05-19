// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Components.WebAssembly.Authentication;

internal sealed class DefaultApiAuthorizationOptionsConfiguration : IPostConfigureOptions<RemoteAuthenticationOptions<ApiAuthorizationProviderOptions>>
{
    private readonly string _applicationName;

    public DefaultApiAuthorizationOptionsConfiguration(string applicationName) => _applicationName = applicationName;

    public void Configure(RemoteAuthenticationOptions<ApiAuthorizationProviderOptions> options)
    {
        options.ProviderOptions.ConfigurationEndpoint ??= $"_configuration/{_applicationName}";
        options.AuthenticationPaths.RemoteRegisterPath ??= "Identity/Account/Register";
        options.AuthenticationPaths.RemoteProfilePath ??= "Identity/Account/Manage";
        options.UserOptions.ScopeClaim ??= "scope";
        options.UserOptions.RoleClaim ??= "role";
        options.UserOptions.AuthenticationType ??= _applicationName;
    }

    public void PostConfigure(string? name, RemoteAuthenticationOptions<ApiAuthorizationProviderOptions> options)
    {
        if (string.Equals(name, Options.DefaultName))
        {
            Configure(options);
        }
    }
}
