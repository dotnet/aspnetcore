// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net;
using System.Net.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Hosting;
using RoutingWebSite;

namespace Microsoft.AspNetCore.Routing.FunctionalTests;

public class EndpointRoutingSampleTest : IDisposable
{
    private readonly HttpClient _client;
    private readonly IHost _host;
    private readonly TestServer _testServer;

    public EndpointRoutingSampleTest()
    {
        var hostBuilder = Program.GetHostBuilder(new[] { Program.EndpointRoutingScenario, });
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
    public async Task MatchesRootPath_AndReturnsPlaintext()
    {
        // Arrange
        var expectedContentType = "text/plain";

        // Act
        var response = await _client.GetAsync("/");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(response.Content);
        Assert.NotNull(response.Content.Headers.ContentType);
        Assert.Equal(expectedContentType, response.Content.Headers.ContentType.MediaType);
    }

    [Fact]
    public async Task MatchesStaticRouteTemplate_AndReturnsPlaintext()
    {
        // Arrange
        var expectedContentType = "text/plain";
        var expectedContent = "Plain text!";

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

    [Fact]
    public async Task MatchesHelloMiddleware_AndReturnsPlaintext()
    {
        // Arrange
        var expectedContentType = "text/plain";
        var expectedContent = "Hello World";

        // Act
        var response = await _client.GetAsync("/helloworld");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(response.Content);
        Assert.NotNull(response.Content.Headers.ContentType);
        Assert.Equal(expectedContentType, response.Content.Headers.ContentType.MediaType);
        var actualContent = await response.Content.ReadAsStringAsync();
        Assert.Equal(expectedContent, actualContent);
    }

    [Fact]
    public async Task MatchesEndpoint_WithSuccessfulConstraintMatch()
    {
        // Arrange
        var expectedContent = "WithConstraints";

        // Act
        var response = await _client.GetAsync("/withconstraints/555_001");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(response.Content);
        var actualContent = await response.Content.ReadAsStringAsync();
        Assert.Equal(expectedContent, actualContent);
    }

    [Fact]
    public async Task DoesNotMatchEndpoint_IfConstraintMatchFails()
    {
        // Arrange & Act
        var response = await _client.GetAsync("/withconstraints/555");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task MatchesEndpoint_WithSuccessful_OptionalConstraintMatch()
    {
        // Arrange
        var expectedContent = "withoptionalconstraints";

        // Act
        var response = await _client.GetAsync("/withoptionalconstraints/555_001");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(response.Content);
        var actualContent = await response.Content.ReadAsStringAsync();
        Assert.Equal(expectedContent, actualContent);
    }

    [Fact]
    public async Task MatchesEndpoint_WithSuccessful_OptionalConstraintMatch_NoValueForParameter()
    {
        // Arrange
        var expectedContent = "withoptionalconstraints";

        // Act
        var response = await _client.GetAsync("/withoptionalconstraints");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(response.Content);
        var actualContent = await response.Content.ReadAsStringAsync();
        Assert.Equal(expectedContent, actualContent);
    }

    [Fact]
    public async Task DoesNotMatchEndpoint_IfOptionalConstraintMatchFails()
    {
        // Arrange & Act
        var response = await _client.GetAsync("/withoptionalconstraints/555");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Theory]
    [InlineData("/WithSingleAsteriskCatchAll/a/b/c", "Link: /WithSingleAsteriskCatchAll/a%2Fb%2Fc")]
    [InlineData("/WithSingleAsteriskCatchAll/a/b b1/c c1", "Link: /WithSingleAsteriskCatchAll/a%2Fb%20b1%2Fc%20c1")]
    public async Task GeneratesLink_ToEndpointWithSingleAsteriskCatchAllParameter_EncodesValue(
        string url,
        string expected)
    {
        // Arrange & Act
        var response = await _client.GetAsync(url);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(response.Content);
        var actualContent = await response.Content.ReadAsStringAsync();
        Assert.Equal(expected, actualContent);
    }

    [Theory]
    [InlineData("/WithDoubleAsteriskCatchAll/a/b/c", "Link: /WithDoubleAsteriskCatchAll/a/b/c")]
    [InlineData("/WithDoubleAsteriskCatchAll/a/b/c/", "Link: /WithDoubleAsteriskCatchAll/a/b/c/")]
    [InlineData("/WithDoubleAsteriskCatchAll/a//b/c", "Link: /WithDoubleAsteriskCatchAll/a//b/c")]
    public async Task GeneratesLink_ToEndpointWithDoubleAsteriskCatchAllParameter_DoesNotEncodeSlashes(
        string url,
        string expected)
    {
        // Arrange & Act
        var response = await _client.GetAsync(url);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(response.Content);
        var actualContent = await response.Content.ReadAsStringAsync();
        Assert.Equal(expected, actualContent);
    }

    [Fact]
    public async Task GeneratesLink_ToEndpointWithDoubleAsteriskCatchAllParameter_EncodesContentOtherThanSlashes()
    {
        // Arrange & Act
        var response = await _client.GetAsync("/WithDoubleAsteriskCatchAll/a/b b1/c c1");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(response.Content);
        var actualContent = await response.Content.ReadAsStringAsync();
        Assert.Equal("Link: /WithDoubleAsteriskCatchAll/a/b%20b1/c%20c1", actualContent);
    }

    [Fact]
    public async Task MapGet_HasConventionMetadata()
    {
        // Arrange & Act
        var response = await _client.GetAsync("/convention");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var actualContent = await response.Content.ReadAsStringAsync();
        Assert.Equal("Has metadata", actualContent);
    }

    public void Dispose()
    {
        _testServer.Dispose();
        _client.Dispose();
        _host.Dispose();
    }
}
