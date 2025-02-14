// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.AspNetCore.InternalTesting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace Microsoft.AspNetCore.Server.IISIntegration;

[SkipOnHelix("Unsupported queue", Queues = "Windows.Amd64.VS2022.Pre.Open;")]
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
