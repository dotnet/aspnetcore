// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Reflection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.InternalTesting;
using Microsoft.AspNetCore.Mvc.Testing;
using RoutingWebSite;
using Xunit.Abstractions;

namespace Microsoft.AspNetCore.Mvc.FunctionalTests;

public class RoutingDynamicOrderTest : LoggedTest
{
    private static void ConfigureWebHostBuilder(IWebHostBuilder builder) => builder.UseStartup<RoutingWebSite.StartupForDynamicOrder>();

    protected override void Initialize(TestContext context, MethodInfo methodInfo, object[] testMethodArguments, ITestOutputHelper testOutputHelper)
    {
        base.Initialize(context, methodInfo, testMethodArguments, testOutputHelper);
        Factory = new MvcTestFixture<RoutingWebSite.StartupForDynamic>(LoggerFactory).WithWebHostBuilder(ConfigureWebHostBuilder);
    }

    public override void Dispose()
    {
        Factory.Dispose();
        base.Dispose();
    }

    public WebApplicationFactory<RoutingWebSite.StartupForDynamic> Factory { get; private set; }

    [Fact]
    public async Task PrefersAttributeRoutesOverDynamicControllerRoutes()
    {
        var factory = Factory
            .WithWebHostBuilder(b => b.UseSetting("Scenario", RoutingWebSite.StartupForDynamicOrder.DynamicOrderScenarios.AttributeRouteDynamicRoute));

        var client = factory.CreateClient();

        // Arrange
        var url = "http://localhost/attribute-dynamic-order/Controller=Home,Action=Index";
        var request = new HttpRequestMessage(HttpMethod.Get, url);

        // Act
        var response = await client.SendAsync(request);
        var content = await response.Content.ReadFromJsonAsync<RouteInfo>();

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("AttributeRouteSlug", content.RouteName);
    }

    [Fact]
    public async Task DynamicRoutesAreMatchedInDefinitionOrderOverPrecedence()
    {
        AppContext.SetSwitch("Microsoft.AspNetCore.Routing.UseCorrectCatchAllBehavior", isEnabled: true);
        var factory = Factory
            .WithWebHostBuilder(b => b.UseSetting("Scenario", RoutingWebSite.StartupForDynamicOrder.DynamicOrderScenarios.MultipleDynamicRoute));

        var client = factory.CreateClient();

        // Arrange
        var url = "http://localhost/dynamic-order/specific/Controller=Home,Action=Index";
        var request = new HttpRequestMessage(HttpMethod.Get, url);

        // Act
        var response = await client.SendAsync(request);
        var content = await response.Content.ReadFromJsonAsync<RouteInfo>();

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True(content.RouteValues.TryGetValue("identifier", out var identifier));
        Assert.Equal("slug", identifier);
    }

    [Fact]
    public async Task ConventionalRoutesDefinedEarlierWinOverDynamicControllerRoutes()
    {
        AppContext.SetSwitch("Microsoft.AspNetCore.Routing.UseCorrectCatchAllBehavior", isEnabled: true);
        var factory = Factory
            .WithWebHostBuilder(b => b.UseSetting("Scenario", RoutingWebSite.StartupForDynamicOrder.DynamicOrderScenarios.ConventionalRouteDynamicRoute));

        var client = factory.CreateClient();

        // Arrange
        var url = "http://localhost/conventional-dynamic-order-before";
        var request = new HttpRequestMessage(HttpMethod.Get, url);

        // Act
        var response = await client.SendAsync(request);
        var content = await response.Content.ReadFromJsonAsync<RouteInfo>();

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.False(content.RouteValues.TryGetValue("identifier", out var identifier));
    }

    [Fact]
    public async Task ConventionalRoutesDefinedLaterLooseToDynamicControllerRoutes()
    {
        AppContext.SetSwitch("Microsoft.AspNetCore.Routing.UseCorrectCatchAllBehavior", isEnabled: true);
        var factory = Factory
            .WithWebHostBuilder(b => b.UseSetting("Scenario", RoutingWebSite.StartupForDynamicOrder.DynamicOrderScenarios.ConventionalRouteDynamicRoute));

        var client = factory.CreateClient();

        // Arrange
        var url = "http://localhost/conventional-dynamic-order-after";
        var request = new HttpRequestMessage(HttpMethod.Get, url);

        // Act
        var response = await client.SendAsync(request);
        var content = await response.Content.ReadFromJsonAsync<RouteInfo>();

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True(content.RouteValues.TryGetValue("identifier", out var identifier));
        Assert.Equal("slug", identifier);
    }

    [Fact]
    public async Task DynamicPagesDefinedEarlierWinOverDynamicControllers()
    {
        AppContext.SetSwitch("Microsoft.AspNetCore.Routing.UseCorrectCatchAllBehavior", isEnabled: true);
        var factory = Factory
            .WithWebHostBuilder(b => b.UseSetting("Scenario", RoutingWebSite.StartupForDynamicOrder.DynamicOrderScenarios.DynamicControllerAndPages));

        var client = factory.CreateClient();
        // Arrange
        var url = "http://localhost/dynamic-order-page-controller-before";
        var request = new HttpRequestMessage(HttpMethod.Get, url);

        // Act
        var response = await client.SendAsync(request);
        var content = await response.Content.ReadAsStringAsync();

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("Hello from dynamic page: /DynamicPagebefore", content);
    }

    [Fact]
    public async Task DynamicPagesDefinedLaterLooseOverDynamicControllers()
    {
        AppContext.SetSwitch("Microsoft.AspNetCore.Routing.UseCorrectCatchAllBehavior", isEnabled: true);
        var factory = Factory
            .WithWebHostBuilder(b => b.UseSetting("Scenario", RoutingWebSite.StartupForDynamicOrder.DynamicOrderScenarios.DynamicControllerAndPages));

        var client = factory.CreateClient();

        // Arrange
        var url = "http://localhost/dynamic-order-page-controller-after";
        var request = new HttpRequestMessage(HttpMethod.Get, url);

        // Act
        var response = await client.SendAsync(request);
        var content = await response.Content.ReadFromJsonAsync<RouteInfo>();

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True(content.RouteValues.TryGetValue("identifier", out var identifier));
        Assert.Equal("controller", identifier);
    }

    private record RouteInfo(string RouteName, IDictionary<string, string> RouteValues);
}
