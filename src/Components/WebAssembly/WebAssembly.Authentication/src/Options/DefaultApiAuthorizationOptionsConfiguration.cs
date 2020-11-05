// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Components.WebAssembly.Authentication
{
    internal class DefaultApiAuthorizationOptionsConfiguration : IPostConfigureOptions<RemoteAuthenticationOptions<ApiAuthorizationProviderOptions>>
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

        public void PostConfigure(string name, RemoteAuthenticationOptions<ApiAuthorizationProviderOptions> options)
        {
            if (string.Equals(name, Options.DefaultName))
            {
                Configure(options);
            }
        }
    }
}
