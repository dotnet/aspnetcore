// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Blazor.Browser.Http;
using Microsoft.AspNetCore.Blazor.Browser.Services;
using Microsoft.AspNetCore.Blazor.Builder;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Runtime.InteropServices;

namespace BasicTestApp
{
    public class Startup
    {
        public void ConfigureServices(IServiceCollection services)
        {
        }

        public void Configure(IBlazorApplicationBuilder app)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Create("WEBASSEMBLY")))
            {
                // Needed because the test server runs on a different port than the client app,
                // and we want to test sending/receiving cookies underling this config
                BrowserHttpMessageHandler.DefaultCredentials = FetchCredentialsOption.Include;

                GC.KeepAlive(ActivateMonoJSRuntime.EnsureActivated());
            }

            app.AddComponent<Index>("root");
        }
    }
}
