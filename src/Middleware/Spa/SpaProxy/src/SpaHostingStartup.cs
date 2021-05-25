// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

[assembly: HostingStartup(typeof(Microsoft.AspNetCore.SpaProxy.SpaHostingStartup))]

namespace Microsoft.AspNetCore.SpaProxy
{
    internal class SpaHostingStartup : IHostingStartup
    {
        public void Configure(IWebHostBuilder builder)
        {
            builder.ConfigureServices(services =>
            {
                var spaProxyConfigFile = Path.Combine(AppContext.BaseDirectory, "spa.proxy.json");
                if (File.Exists(spaProxyConfigFile))
                {
                    var configuration = new ConfigurationBuilder()
                        .AddJsonFile(spaProxyConfigFile)
                        .Build();

                    services.AddSingleton<SpaProxyLaunchManager>();
                    services.Configure<SpaDevelopmentServerOptions>(configuration.GetSection("SpaProxyServer"));
                    services.TryAddEnumerable(ServiceDescriptor.Singleton<IStartupFilter, SpaProxyStartupFilter>());
                }
            });
        }
    }
}
