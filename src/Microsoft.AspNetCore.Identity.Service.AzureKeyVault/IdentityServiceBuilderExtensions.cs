// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Identity.Service.AzureKeyVault
{
    public static class IdentityServiceBuilderExtensions
    {
        public static IIdentityServiceBuilder AddKeyVault(this IIdentityServiceBuilder builder)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            var services = builder.Services;
            services.TryAddEnumerable(ServiceDescriptor.Singleton<IConfigureOptions<KeyVaultSigningCredentialsSourceOptions>, DefaultSetup>());
            services.TryAddSingleton<ISigningCredentialsSource, KeyVaultSigningCredentialSource>();
            return builder;
        }

        public static IIdentityServiceBuilder AddKeyVault(this IIdentityServiceBuilder builder, Action<KeyVaultSigningCredentialsSourceOptions> configure)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            if (configure == null)
            {
                throw new ArgumentNullException(nameof(configure));
            }

            builder.Services.Configure(configure);
            builder.Services.TryAddSingleton<ISigningCredentialsSource, KeyVaultSigningCredentialSource>();

            return builder;
        }

        private class DefaultSetup : ConfigureOptions<KeyVaultSigningCredentialsSourceOptions>
        {
            public DefaultSetup(IConfiguration configuration)
                : base(options => configuration.GetSection("Identity:KeyVault").Bind(options)) { }
        }
    }
}
