// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;

namespace Microsoft.AspNetCore.Mvc.FunctionalTests;

public class RealServerBackedIntegrationTests : IClassFixture<KestrelBasedWebApplicationFactory>
{
    public KestrelBasedWebApplicationFactory Factory { get; }

    public RealServerBackedIntegrationTests(KestrelBasedWebApplicationFactory factory)
    {
        Factory = factory;
    }

    [Fact]
    public async Task RetrievesDataFromRealServer()
    {
        // Arrange
        var expectedMediaType = MediaTypeHeaderValue.Parse("application/json; charset=utf-8");

        // Act
        var client = Factory.CreateClient();
        var response = await client.GetAsync("/");
        var responseContent = await response.Content.ReadAsStringAsync();

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(expectedMediaType, response.Content.Headers.ContentType);

        Assert.Contains("first", responseContent);
        Assert.Contains("second", responseContent);
        Assert.Contains("wall", responseContent);
        Assert.Contains("floor", responseContent);
    }

    [Fact]
    public async Task ServerReachableViaGenericHttpClient()
    {
        // Arrange
        var baseAddress = new Uri("http://localhost:5000");

        // Act
        using var factoryClient = Factory.CreateClient();
        using var client = new HttpClient() { BaseAddress = factoryClient.BaseAddress };

        using var response = await client.GetAsync("/");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
