// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net;
using System.Net.Http;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Microsoft.AspNetCore.Routing.FunctionalTests;

public class RouterBenchmarkTest : IDisposable
{
    private readonly HttpClient _client;
    private readonly IHost _host;
    private readonly TestServer _testServer;

    public RouterBenchmarkTest()
    {
        // This switch and value are set by benchmark server when running the app for profiling.
        var args = new[] { "--scenarios", "PlaintextRouting" };
        var hostBuilder = Benchmarks.Program.GetHostBuilder(args);

        _host = hostBuilder.Build();

        // Make sure we are using the right startup
        var configuration = _host.Services.GetService<IConfiguration>();
        var startupName = configuration["Startup"];
        Assert.Equal(nameof(Benchmarks.StartupUsingRouter), startupName);

        _testServer = _host.GetTestServer();
        _host.Start();
        _client = _testServer.CreateClient();
        _client.BaseAddress = new Uri("http://localhost");
    }

    [Fact]
    public async Task RouteHandlerWritesResponse()
    {
        // Arrange
        var expectedContentType = "text/plain";
        var expectedContent = "Hello, World!";

        // Act
        var response = await _client.GetAsync("/plaintext");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(response.Content);
        Assert.NotNull(response.Content.Headers.ContentType);
        Assert.Equal(expectedContentType, response.Content.Headers.ContentType.MediaType);
        var actualContent = await response.Content.ReadAsStringAsync();
        Assert.Equal(expectedContent, actualContent);
    }

    public void Dispose()
    {
        _testServer.Dispose();
        _client.Dispose();
        _host.Dispose();
    }
}
