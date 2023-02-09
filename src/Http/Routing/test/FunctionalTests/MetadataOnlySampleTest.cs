// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net;
using System.Net.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Hosting;
using RoutingWebSite;

namespace Microsoft.AspNetCore.Routing.FunctionalTests;

public class MetadataOnlySampleTest : IDisposable
{
    private readonly HttpClient _client;
    private readonly IHost _host;
    private readonly TestServer _testServer;

    public MetadataOnlySampleTest()
    {
        var hostBuilder = Program.GetHostBuilder(new[] { Program.MetadataOnlyScenario, });
        _host = hostBuilder.Build();

        _testServer = _host.GetTestServer();
        _host.Start();

        _client = _testServer.CreateClient();
        _client.BaseAddress = new Uri("http://localhost");
    }

    [Fact]
    public async Task EndpointCombinesMetadata()
    {
        // Arrange & Act
        var response = await _client.GetAsync("/printmeta");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var actualContent = await response.Content.ReadAsStringAsync();
        Assert.Contains("This is on every endpoint now!", actualContent);
        Assert.Contains("This is only on this single endpoint", actualContent);
    }

    public void Dispose()
    {
        _testServer.Dispose();
        _client.Dispose();
        _host.Dispose();
    }
}
