// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Runtime.InteropServices;
using BasicTestApp.AuthTest;
using Microsoft.AspNetCore.Blazor.Http;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace BasicTestApp
{
    public class Startup
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddSingleton<AuthenticationStateProvider, ServerAuthenticationStateProvider>();

            services.AddAuthorizationCore(options =>
            {
                options.AddPolicy("NameMustStartWithB", policy =>
                    policy.RequireAssertion(ctx => ctx.User.Identity.Name?.StartsWith("B") ?? false));
            });
        }

        public void Configure(IComponentsApplicationBuilder app)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Create("WEBASSEMBLY")))
            {
                // Needed because the test server runs on a different port than the client app,
                // and we want to test sending/receiving cookies underling this config
                WebAssemblyHttpMessageHandler.DefaultCredentials = FetchCredentialsOption.Include;
            }

            app.AddComponent<Index>("root");
        }
    }
}
