// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Hosting;

namespace Microsoft.AspNetCore.TestHost;

public class RequestBuilderTests
{
    [Fact]
    public void AddRequestHeader()
    {
        var builder = new WebHostBuilder().Configure(app => { });
        var server = new TestServer(builder);
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
        var builder = new WebHostBuilder().Configure(app => { });
        var server = new TestServer(builder);
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
        var builder = new WebHostBuilder().Configure(app => { });
        var server = new TestServer(builder);

        // Act
        var requestBuilder = server.CreateRequest("/");

        // Assert
        Assert.Equal(server, requestBuilder.TestServer);
    }
}
