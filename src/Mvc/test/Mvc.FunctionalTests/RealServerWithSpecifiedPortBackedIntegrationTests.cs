// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;

namespace Microsoft.AspNetCore.Mvc.FunctionalTests;

public class RealServerWithSpecifiedPortBackedIntegrationTests : IClassFixture<KestrelBasedWebApplicationFactory>
{
    private const int ServerPort = 7777;

    public KestrelBasedWebApplicationFactory Factory { get; }

    public RealServerWithSpecifiedPortBackedIntegrationTests(KestrelBasedWebApplicationFactory factory)
    {
        Factory = factory;
        Factory.UseKestrel(ServerPort);
    }

    [Fact]
    public async Task ServerReachableViaGenericHttpClient_OnSpecificPort()
    {
        // Arrange
        var baseAddress = new Uri($"http://localhost:{ServerPort}");

        // Act
        Factory.StartServer();

        using var client = new HttpClient() { BaseAddress = baseAddress };

        using var response = await client.GetAsync("/");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
