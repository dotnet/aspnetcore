// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;

[assembly: HostingStartup(typeof(Microsoft.AspNetCore.SpaProxy.SpaHostingStartup))]

namespace Microsoft.AspNetCore.SpaProxy
{
    internal class SpaHostingStartup : IHostingStartup
    {
        public void Configure(IWebHostBuilder builder)
        {
            builder.ConfigureServices(services =>
            {
                if (File.Exists(Path.Combine(AppContext.BaseDirectory, "spa.proxy.json")))
                {
                    services.AddHostedService<SpaProxyLaunchManager>();
                }
            });
        }
    }
}
