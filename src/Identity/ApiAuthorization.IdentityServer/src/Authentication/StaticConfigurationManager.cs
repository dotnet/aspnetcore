// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;

namespace Microsoft.AspNetCore.ApiAuthorization.IdentityServer
{
    internal class StaticConfigurationManager : IConfigurationManager<OpenIdConnectConfiguration>
    {
        private readonly Task<OpenIdConnectConfiguration> _configuration;

        public StaticConfigurationManager(OpenIdConnectConfiguration configuration) => _configuration = Task.FromResult(configuration);

        public Task<OpenIdConnectConfiguration> GetConfigurationAsync(CancellationToken cancel) => _configuration;

        public void RequestRefresh()
        {
        }
    }
}
