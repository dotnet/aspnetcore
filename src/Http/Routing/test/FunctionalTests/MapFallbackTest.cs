// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net;
using System.Net.Http;
using RoutingWebSite;

namespace Microsoft.AspNetCore.Routing.FunctionalTests;

public class MapFallbackTest : IClassFixture<RoutingTestFixture<MapFallbackStartup>>
{
    private readonly RoutingTestFixture<MapFallbackStartup> _fixture;
    private readonly HttpClient _client;

    public MapFallbackTest(RoutingTestFixture<MapFallbackStartup> fixture)
    {
        _fixture = fixture;
        _client = _fixture.CreateClient("http://localhost");
    }

    [Fact]
    public async Task Get_HelloWorld()
    {
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Get, "helloworld");

        // Act
        var response = await _client.SendAsync(request);
        var responseContent = await response.Content.ReadAsStringAsync();

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("Hello World", responseContent);
    }

    [Theory]
    [InlineData("prefix/favicon.ico")]
    [InlineData("prefix/content/js/jquery.min.js")]
    public async Task Get_FallbackWithPattern_FileName(string path)
    {
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Get, path);

        // Act
        var response = await _client.SendAsync(request);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Theory]
    [InlineData("prefix")]
    [InlineData("prefix/")]
    [InlineData("prefix/store")]
    [InlineData("prefix/blog/read/18")]
    public async Task Get_FallbackWithPattern_NonFileName(string path)
    {
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Get, path);

        // Act
        var response = await _client.SendAsync(request);
        var responseContent = await response.Content.ReadAsStringAsync();

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("FallbackCustomPattern", responseContent);
    }

    [Theory]
    [InlineData("favicon.ico")]
    [InlineData("content/js/jquery.min.js")]
    public async Task Get_Fallback_FileName(string path)
    {
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Get, path);

        // Act
        var response = await _client.SendAsync(request);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Theory]
    [InlineData("")]
    [InlineData("/")]
    [InlineData("store")]
    [InlineData("blog/read/18")]
    public async Task Get_Fallback_NonFileName(string path)
    {
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Get, path);

        // Act
        var response = await _client.SendAsync(request);
        var responseContent = await response.Content.ReadAsStringAsync();

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("FallbackDefaultPattern", responseContent);
    }
}
