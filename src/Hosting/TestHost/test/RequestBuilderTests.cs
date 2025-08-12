// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;

namespace Microsoft.AspNetCore.TestHost;

public class RequestBuilderTests
{
    [Fact]
    public void AddRequestHeader()
    {
        using var host = new HostBuilder()
            .ConfigureWebHost(webHostBuilder =>
            {
                webHostBuilder
                    .UseTestServer()
                    .Configure(app => { });
            })
            .Build();
        var server = host.GetTestServer();
        server.CreateRequest("/")
            .AddHeader("Host", "MyHost:90")
            .And(request =>
            {
                Assert.Equal("MyHost:90", request.Headers.Host.ToString());
            });
    }

    [Fact]
    public void AddContentHeaders()
    {
        using var host = new HostBuilder()
            .ConfigureWebHost(webHostBuilder =>
            {
                webHostBuilder
                    .UseTestServer()
                    .Configure(app => { });
            })
            .Build();
        var server = host.GetTestServer();
        server.CreateRequest("/")
            .AddHeader("Content-Type", "Test/Value")
            .And(request =>
            {
                Assert.NotNull(request.Content);
                Assert.Equal("Test/Value", request.Content.Headers.ContentType.ToString());
            });
    }

    [Fact]
    public void TestServer_PropertyShouldHoldTestServerInstance()
    {
        // Arrange
        using var host = new HostBuilder()
            .ConfigureWebHost(webHostBuilder =>
            {
                webHostBuilder
                    .UseTestServer()
                    .Configure(app => { });
            })
            .Build();
        var server = host.GetTestServer();

        // Act
        var requestBuilder = server.CreateRequest("/");

        // Assert
        Assert.Equal(server, requestBuilder.TestServer);
    }
}
