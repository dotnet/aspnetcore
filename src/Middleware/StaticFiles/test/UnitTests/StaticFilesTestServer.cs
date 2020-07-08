// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Microsoft.AspNetCore.StaticFiles
{
    public static class StaticFilesTestServer
    {
        public static async Task<IHost> Create(Action<IApplicationBuilder> configureApp, Action<IServiceCollection> configureServices = null)
        {
            Action<IServiceCollection> defaultConfigureServices = services => { };
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new []
                {
                    new KeyValuePair<string, string>("webroot", ".")
                })
                .Build();
            var host = new HostBuilder()
                .ConfigureWebHost(webHostBuilder =>
                {
                    webHostBuilder
                    .UseTestServer()
                    .UseConfiguration(configuration)
                    .Configure(configureApp)
                    .ConfigureServices(configureServices ?? defaultConfigureServices);
                }).Build();

            await host.StartAsync();
            return host;
        }
    }
}
