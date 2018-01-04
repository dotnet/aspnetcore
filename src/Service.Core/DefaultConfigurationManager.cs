// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;

namespace Microsoft.AspNetCore.Identity.Service
{
    public class DefaultConfigurationManager : IConfigurationManager
    {
        private readonly IConfigurationMetadataProvider[] _providers;
        private readonly ConcurrentDictionary<string, Lazy<Task<OpenIdConnectConfiguration>>> _configurations =
            new ConcurrentDictionary<string, Lazy<Task<OpenIdConnectConfiguration>>>();

        public DefaultConfigurationManager(
            IEnumerable<IConfigurationMetadataProvider> providers)
        {
            _providers = providers.OrderBy(p => p.Order).ToArray();
        }

        public async Task<OpenIdConnectConfiguration> GetConfigurationAsync(ConfigurationContext context)
        {
            return await _configurations.GetOrAdd(
                context.Id,
                new Lazy<Task<OpenIdConnectConfiguration>>(CreateConfiguration)).Value;

            async Task<OpenIdConnectConfiguration> CreateConfiguration()
            {
                var configuration = new OpenIdConnectConfiguration();
                foreach (var provider in _providers)
                {
                    await provider.ConfigureMetadataAsync(configuration, context);
                }

                return configuration;
            }
        }
    }
}
