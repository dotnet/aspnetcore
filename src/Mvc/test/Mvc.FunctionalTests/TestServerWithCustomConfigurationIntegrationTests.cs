// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;

namespace Microsoft.AspNetCore.Mvc.FunctionalTests;

public class CustomWebApplicationFactory : WebApplicationFactory<SimpleWebSite.Startup>
{
    public Action<TestServerOptions> ConfigureOptions { get; set; }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseTestServer(ConfigureOptions);
    }
}

public class TestServerWithCustomConfigurationIntegrationTests : IClassFixture<CustomWebApplicationFactory>
{
    public CustomWebApplicationFactory Factory { get; }

    public TestServerWithCustomConfigurationIntegrationTests(CustomWebApplicationFactory factory)
    {
        Factory = factory;
    }

    [Fact]
    public async Task ServerConfigured()
    {
        // Arrange
        var optionsConfiguredCounter = 0;
        Factory.ConfigureOptions = options =>
        {
            options.AllowSynchronousIO = true;
            options.PreserveExecutionContext = true;
            optionsConfiguredCounter++;
        };

        // Act
        Assert.Equal(0, optionsConfiguredCounter);
        Factory.StartServer();

        using var client = Factory.CreateClient();

        using var response = await client.GetAsync("/");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(1, optionsConfiguredCounter);
        Assert.True(Factory.Server.AllowSynchronousIO);
        Assert.True(Factory.Server.PreserveExecutionContext);
    }
}
