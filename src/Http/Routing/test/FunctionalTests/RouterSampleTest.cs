// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net;
using System.Net.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Hosting;
using RoutingWebSite;

namespace Microsoft.AspNetCore.Routing.FunctionalTests;

public class RouterSampleTest : IDisposable
{
    private readonly HttpClient _client;
    private readonly IHost _host;
    private readonly TestServer _testServer;

    public RouterSampleTest()
    {
        var hostBuilder = Program.GetHostBuilder(new[] { Program.RouterScenario, });
        _host = hostBuilder.Build();
        _testServer = _host.GetTestServer();
        _host.Start();
        _client = _testServer.CreateClient();
        _client.BaseAddress = new Uri("http://localhost");
    }

    [Theory]
    [InlineData("Branch1")]
    [InlineData("Branch2")]
    public async Task Routing_CanRouteRequest_ToBranchRouter(string branch)
    {
        // Arrange
        var message = new HttpRequestMessage(HttpMethod.Get, $"{branch}/api/get/5");

        // Act
        var response = await _client.SendAsync(message);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal($"{branch} - API Get 5", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Routing_CanRouteRequestDelegate_ToSpecificHttpVerb()
    {
        // Arrange
        var message = new HttpRequestMessage(HttpMethod.Get, "api/get/5");

        // Act
        var response = await _client.SendAsync(message);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal($"API Get 5", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Routing_CanRouteRequest_ToSpecificMiddleware()
    {
        // Arrange
        var message = new HttpRequestMessage(HttpMethod.Get, "api/middleware");

        // Act
        var response = await _client.SendAsync(message);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal($"Middleware!", await response.Content.ReadAsStringAsync());
    }

    [Theory]
    [InlineData("GET")]
    [InlineData("POST")]
    [InlineData("PUT")]
    [InlineData("PATCH")]
    [InlineData("DELETE")]
    [InlineData("HEAD")]
    [InlineData("OPTIONS")]
    public async Task Routing_CanRouteRequest_ToDefaultHandler(string httpVerb)
    {
        // Arrange
        var message = new HttpRequestMessage(new HttpMethod(httpVerb), "api/all/Joe/Duf");
        var expectedBody = $"Verb =  {httpVerb} - Path = /api/all/Joe/Duf - Route values - [name, Joe], [lastName, Duf]";

        // Act
        var response = await _client.SendAsync(message);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadAsStringAsync();
        Assert.Equal(expectedBody, body);
    }

    public void Dispose()
    {
        _testServer.Dispose();
        _client.Dispose();
        _host.Dispose();
    }
}
