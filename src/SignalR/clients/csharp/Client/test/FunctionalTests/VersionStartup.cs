// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.SignalR.Protocol;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;

namespace Microsoft.AspNetCore.SignalR.Client.FunctionalTests
{
    public class VersionStartup
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddSignalR(options =>
            {
                options.EnableDetailedErrors = true;
            });

            services.RemoveAll<IHubProtocol>();
            services.TryAddEnumerable(ServiceDescriptor.Singleton<IHubProtocol>(new VersionedJsonHubProtocol(1000)));

            services.AddAuthentication();
        }

        public void Configure(IApplicationBuilder app)
        {
            app.UseAuthentication();

            app.UseSignalR(routes =>
            {
                routes.MapHub<VersionHub>("/version");
            });
        }
    }
}
