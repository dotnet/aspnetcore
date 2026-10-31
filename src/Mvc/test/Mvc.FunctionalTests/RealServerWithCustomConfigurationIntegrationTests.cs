// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;

namespace Microsoft.AspNetCore.Mvc.FunctionalTests;

public class RealServerWithCustomConfigurationIntegrationTests : IClassFixture<KestrelBasedWebApplicationFactory>
{
    private int _optionsConfiguredCounter = 0;

    public KestrelBasedWebApplicationFactory Factory { get; }

    public RealServerWithCustomConfigurationIntegrationTests(KestrelBasedWebApplicationFactory factory)
    {
        Factory = factory;
        Factory.UseKestrel(options => { _optionsConfiguredCounter++; });
    }

    [Fact]
    public async Task ServerReachableViaGenericHttpClient()
    {
        // Arrange

        // Act
        Assert.Equal(0, _optionsConfiguredCounter);
        Factory.StartServer();

        using var client = Factory.CreateClient();

        using var response = await client.GetAsync("/");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(1, _optionsConfiguredCounter);
    }
}
