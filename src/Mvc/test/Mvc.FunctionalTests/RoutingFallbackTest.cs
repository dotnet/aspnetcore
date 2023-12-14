// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net;
using System.Net.Http;
using System.Text;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Microsoft.AspNetCore.Mvc.FunctionalTests;

public class RoutingFallbackTest : IClassFixture<MvcTestFixture<RoutingWebSite.StartupForFallback>>
{
    public RoutingFallbackTest(MvcTestFixture<RoutingWebSite.StartupForFallback> fixture)
    {
        var factory = fixture.Factories.FirstOrDefault() ?? fixture.WithWebHostBuilder(ConfigureWebHostBuilder);
        Client = factory.CreateDefaultClient();
    }

    private static void ConfigureWebHostBuilder(IWebHostBuilder builder) => builder.UseStartup<RoutingWebSite.StartupForFallback>();

    public HttpClient Client { get; }

    [Fact]
    public async Task Fallback_CanGet404ForMissingFile()
    {
        // Arrange
        var url = "http://localhost/pranav.jpg";
        var request = new HttpRequestMessage(HttpMethod.Get, url);

        // Act
        var response = await Client.SendAsync(request);
        var content = await response.Content.ReadAsStringAsync();

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Fallback_CanAccessKnownEndpoint()
    {
        // Arrange
        var url = "http://localhost/Edit/17";
        var request = new HttpRequestMessage(HttpMethod.Get, url);

        // Act
        var response = await Client.SendAsync(request);
        var content = await response.Content.ReadAsStringAsync();

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("Hello from Edit page", content.Trim());
    }

    [Fact]
    public async Task Fallback_CanFallbackToControllerInArea()
    {
        // Arrange
        var url = "http://localhost/Admin/Foo";
        var request = new HttpRequestMessage(HttpMethod.Get, url);

        // Act
        var response = await Client.SendAsync(request);
        var content = await response.Content.ReadAsStringAsync();

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("Hello from fallback controller: /link_generation/Admin/Fallback/Index", content);
    }

    [Fact]
    public async Task Fallback_CanFallbackToControllerInArea_WithActionConstraints()
    {
        // Arrange
        var url = "http://localhost/Admin/Foo";
        var request = new HttpRequestMessage(HttpMethod.Post, url);

        // Act
        var response = await Client.SendAsync(request);
        var content = await response.Content.ReadAsStringAsync();

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("Hello from fallback controller POST: /link_generation/Admin/Fallback/Index", content);
    }

    [Fact]
    public async Task Fallback_CanFallbackToControllerInAreaPost()
    {
        // Arrange
        var url = "http://localhost/Admin/Foo";
        var request = new HttpRequestMessage(HttpMethod.Post, url);

        // Act
        var response = await Client.SendAsync(request);
        var content = await response.Content.ReadAsStringAsync();

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("Hello from fallback controller POST: /link_generation/Admin/Fallback/Index", content);
    }

    [Fact]
    public async Task Fallback_CanFallbackToPage()
    {
        // Arrange
        var url = "http://localhost/FallbackToPage/Foo/Bar";
        var request = new HttpRequestMessage(HttpMethod.Get, url);

        // Act
        var response = await Client.SendAsync(request);
        var content = await response.Content.ReadAsStringAsync();

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("Hello from fallback page: /FallbackPage", content);
    }

    [Fact]
    public async Task Fallback_DoesNotFallbackToFile_WhenContentTypeDoesNotMatchConsumesAttribute()
    {
        // Arrange
        var url = "http://localhost/ConsumesAttribute/Json";
        var request = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = new StringContent("some plaintext", Encoding.UTF8, "text/plain"),
        };

        // Act
        var response = await Client.SendAsync(request);

        // Assert
        Assert.Equal(HttpStatusCode.UnsupportedMediaType, response.StatusCode);
    }
}
