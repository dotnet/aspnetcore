// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace Microsoft.AspNetCore.Server.IISIntegration
{
    public class IISExtensionTests
    {
        [Fact]
        public async Task CallingUseIISIntegrationMultipleTimesWorks()
        {
            using var host = new HostBuilder()
                .ConfigureWebHost(webHostBuilder =>
                {
                    webHostBuilder
                        .UseSetting("TOKEN", "TestToken")
                        .UseSetting("PORT", "12345")
                        .UseSetting("APPL_PATH", "/")
                        .UseIISIntegration()
                        .UseIISIntegration()
                        .Configure(app => { })
                        .UseTestServer();
                })
                .Build();

            var server = host.GetTestServer();

            await host.StartAsync();

            var filters = server.Services.GetServices<IStartupFilter>()
                .OfType<IISSetupFilter>();

            Assert.Single(filters);
        }
    }
}
