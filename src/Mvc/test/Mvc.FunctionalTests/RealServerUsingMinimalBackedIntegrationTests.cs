// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Mvc.FunctionalTests;

public class RealServerUsingMinimalBackedIntegrationTests : IClassFixture<KestrelBasedWebApplicationFactoryForMinimal>
{
    public KestrelBasedWebApplicationFactoryForMinimal Factory { get; }

    public RealServerUsingMinimalBackedIntegrationTests(KestrelBasedWebApplicationFactoryForMinimal factory)
    {
        Factory = factory;
    }

    [Fact]
    public async Task RetrievesDataFromRealServer()
    {
        // Arrange
        var expectedMediaType = MediaTypeHeaderValue.Parse("text/plain; charset=utf-8");

        // Act
        var client = Factory.CreateClient();
        var response = await client.GetAsync("/");
        var responseContent = await response.Content.ReadAsStringAsync();

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(expectedMediaType, response.Content.Headers.ContentType);

        Assert.Contains("Hello World", responseContent);
    }

    [Fact]
    public async Task ServerReachableViaGenericHttpClient()
    {
        // Act
        using var factoryClient = Factory.CreateClient();
        using var client = new HttpClient() { BaseAddress = factoryClient.BaseAddress };

        using var response = await client.GetAsync("/");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public void CanResolveServices()
    {
        // Act
        var server = Factory.Services.GetRequiredService<IServer>();

        // Assert
        Assert.NotNull(server);
        Assert.Contains("Kestrel", server.GetType().FullName);
    }
}
