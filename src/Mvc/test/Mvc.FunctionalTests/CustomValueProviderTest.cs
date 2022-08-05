// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net;
using System.Net.Http;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Mvc.FunctionalTests;

public class CustomValueProviderTest : IClassFixture<MvcTestFixture<BasicWebSite.StartupWithCustomValueProvider>>
{
    private IServiceCollection _serviceCollection;

    public CustomValueProviderTest(MvcTestFixture<BasicWebSite.StartupWithCustomValueProvider> fixture)
    {
        var factory = fixture.Factories.FirstOrDefault() ?? fixture.WithWebHostBuilder(b => b.UseStartup<BasicWebSite.StartupWithCustomValueProvider>());
        factory = factory.WithWebHostBuilder(b => b.ConfigureTestServices(serviceCollection => _serviceCollection = serviceCollection));

        Client = factory.CreateDefaultClient();
    }

    public HttpClient Client { get; }

    [Fact]
    public async Task CustomValueProvider_DisplayName()
    {
        // Arrange
        var url = "http://localhost/CustomValueProvider/CustomValueProviderDisplayName";
        var request = new HttpRequestMessage(HttpMethod.Get, url);

        // Act
        var response = await Client.SendAsync(request);
        var content = await response.Content.ReadAsStringAsync();

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("text/plain", response.Content.Headers.ContentType.MediaType);
        Assert.Equal("BasicWebSite.Controllers.CustomValueProviderController.CustomValueProviderDisplayName (BasicWebSite)", content);
    }

    [Fact]
    public async Task CustomValueProvider_IntValues()
    {
        // Arrange
        var url = "http://localhost/CustomValueProvider/CustomValueProviderIntValues";
        var request = new HttpRequestMessage(HttpMethod.Get, url);

        // Act
        var response = await Client.SendAsync(request);
        var content = await response.Content.ReadAsStringAsync();

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("application/json", response.Content.Headers.ContentType.MediaType);
        Assert.Equal("[42,100,200]", content);
    }

    [Fact]
    public async Task CustomValueProvider_NullableIntValues()
    {
        // Arrange
        var url = "http://localhost/CustomValueProvider/CustomValueProviderNullableIntValues";
        var request = new HttpRequestMessage(HttpMethod.Get, url);

        // Act
        var response = await Client.SendAsync(request);
        var content = await response.Content.ReadAsStringAsync();

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("application/json", response.Content.Headers.ContentType.MediaType);
        Assert.Equal("[null,42,null,100,null,200]", content);
    }

    [Fact]
    public async Task CustomValueProvider_StringValues()
    {
        // Arrange
        var url = "http://localhost/CustomValueProvider/CustomValueProviderStringValues";
        var request = new HttpRequestMessage(HttpMethod.Get, url);

        // Act
        var response = await Client.SendAsync(request);
        var content = await response.Content.ReadAsStringAsync();

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("application/json", response.Content.Headers.ContentType.MediaType);
        Assert.Equal(@"[null,""foo"",null,""bar"",null,""baz""]", content);
    }
}
