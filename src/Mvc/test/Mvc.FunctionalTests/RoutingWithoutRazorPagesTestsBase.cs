// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net;
using System.Net.Http;
using Microsoft.AspNetCore.Hosting;
using Newtonsoft.Json;

namespace Microsoft.AspNetCore.Mvc.FunctionalTests;

public abstract class RoutingWithoutRazorPagesTestsBase<TStartup> : IClassFixture<MvcTestFixture<TStartup>> where TStartup : class
{
    protected RoutingWithoutRazorPagesTestsBase(MvcTestFixture<TStartup> fixture)
    {
        var factory = fixture.Factories.FirstOrDefault() ?? fixture.WithWebHostBuilder(ConfigureWebHostBuilder);
        Client = factory.CreateDefaultClient();
    }

    private static void ConfigureWebHostBuilder(IWebHostBuilder builder) =>
        builder.UseStartup<TStartup>();

    public HttpClient Client { get; }

    [Fact]
    public async Task AttributeRoutedAction_ContainsPage_RouteMatched()
    {
        // Arrange & Act
        var response = await Client.GetAsync("http://localhost/PageRoute/Attribute/pagevalue");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadAsStringAsync();
        var result = JsonConvert.DeserializeObject<RoutingResult>(body);

        Assert.Contains("/PageRoute/Attribute/pagevalue", result.ExpectedUrls);
        Assert.Equal("PageRoute", result.Controller);
        Assert.Equal("AttributeRoute", result.Action);

        Assert.Contains(
           new KeyValuePair<string, object>("page", "pagevalue"),
           result.RouteValues);
    }

    [Fact]
    public async Task ConventionalRoutedAction_RouteContainsPage_RouteNotMatched()
    {
        // Arrange & Act
        var response = await Client.GetAsync("http://localhost/PageRoute/ConventionalRoute/pagevalue");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadAsStringAsync();
        var result = JsonConvert.DeserializeObject<RoutingResult>(body);

        Assert.Equal("PageRoute", result.Controller);
        Assert.Equal("ConventionalRoute", result.Action);

        Assert.Equal("pagevalue", result.RouteValues["page"]);
    }
}
