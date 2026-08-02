// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net;
using System.Net.Http;
using System.Reflection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.InternalTesting;
using Microsoft.AspNetCore.Mvc.Testing;
using RoutingWebSite;
using Xunit.Abstractions;

namespace Microsoft.AspNetCore.Mvc.FunctionalTests;

public class RoutingDynamicTest : LoggedTest
{
    private static void ConfigureWebHostBuilder(IWebHostBuilder builder) => builder.UseStartup<RoutingWebSite.StartupForDynamic>();

    protected override void Initialize(TestContext context, MethodInfo methodInfo, object[] testMethodArguments, ITestOutputHelper testOutputHelper)
    {
        base.Initialize(context, methodInfo, testMethodArguments, testOutputHelper);
        Factory = new MvcTestFixture<RoutingWebSite.StartupForDynamic>(LoggerFactory).WithWebHostBuilder(ConfigureWebHostBuilder);
        Client = Factory.CreateDefaultClient();
    }

    public override void Dispose()
    {
        Factory.Dispose();
        base.Dispose();
    }

    public WebApplicationFactory<RoutingWebSite.StartupForDynamic> Factory { get; private set; }
    public HttpClient Client { get; private set; }

    [Fact]
    public async Task DynamicController_CanGet404ForMissingAction()
    {
        // Arrange
        var url = "http://localhost/dynamic/controller%3DFake,action%3DIndex";
        var request = new HttpRequestMessage(HttpMethod.Get, url);

        // Act
        var response = await Client.SendAsync(request);
        var content = await response.Content.ReadAsStringAsync();

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task DynamicPage_CanGet404ForMissingAction()
    {
        // Arrange
        var url = "http://localhost/dynamicpage/page%3D%2FFake";
        var request = new HttpRequestMessage(HttpMethod.Get, url);

        // Act
        var response = await Client.SendAsync(request);
        var content = await response.Content.ReadAsStringAsync();

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task DynamicController_CanSelectControllerInArea()
    {
        // Arrange
        var url = "http://localhost/v1/dynamic/area%3Dadmin,controller%3Ddynamic,action%3Dindex";
        var request = new HttpRequestMessage(HttpMethod.Get, url);

        // Act
        var response = await Client.SendAsync(request);
        var content = await response.Content.ReadAsStringAsync();

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("Hello from dynamic controller: /link_generation/dynamic/index", content);
    }

    [Fact]
    public async Task DynamicController_CanFilterResultsBasedOnState()
    {
        // Arrange
        var url = "http://localhost/v2/dynamic/area%3Dadmin,controller%3Ddynamic,action%3Dindex";
        var request = new HttpRequestMessage(HttpMethod.Get, url);

        // Act
        var response = await Client.SendAsync(request);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task DynamicController_CanSelectControllerInArea_WithActionConstraints()
    {
        // Arrange
        var url = "http://localhost/v1/dynamic/area%3Dadmin,controller%3Ddynamic,action%3Dindex";
        var request = new HttpRequestMessage(HttpMethod.Post, url);

        // Act
        var response = await Client.SendAsync(request);
        var content = await response.Content.ReadAsStringAsync();

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("Hello from dynamic controller POST: /link_generation/dynamic/index", content);
    }

    [Fact]
    public async Task DynamicPage_CanSelectPage()
    {
        // Arrange
        var url = "http://localhost/v1/dynamicpage/page%3D%2FDynamicPage";
        var request = new HttpRequestMessage(HttpMethod.Get, url);

        // Act
        var response = await Client.SendAsync(request);
        var content = await response.Content.ReadAsStringAsync();

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("Hello from dynamic page: /DynamicPage", content);
    }

    [Fact]
    public async Task DynamicPage_CanFilterBasedOnState()
    {
        // Arrange
        var url = "http://localhost/v2/dynamicpage/page%3D%2FDynamicPage";
        var request = new HttpRequestMessage(HttpMethod.Get, url);

        // Act
        var response = await Client.SendAsync(request);
        var content = await response.Content.ReadAsStringAsync();

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task AppWithDynamicRouteAndMapRazorPages_CanRouteToRazorPage()
    {
        // Regression test for https://github.com/dotnet/aspnetcore/issues/13996
        // Arrange
        var client = Factory.WithWebHostBuilder(b => b.UseStartup<StartupForDynamicAndRazorPages>()).CreateDefaultClient();
        var url = "/PageWithLinks";

        // Act
        var response = await client.GetAsync(url);

        // Assert
        var document = await response.GetHtmlDocumentAsync();
        var editLink = document.RequiredQuerySelector("#editlink");
        Assert.Equal("/Edit/10", editLink.GetAttribute("href"));
    }

    [Fact]
    public async Task AppWithDynamicRouteAndMapRazorPages_CanRouteToDynamicController()
    {
        // Regression test for https://github.com/dotnet/aspnetcore/issues/13996
        // Arrange
        var client = Factory.WithWebHostBuilder(b => b.UseStartup<StartupForDynamicAndRazorPages>()).CreateDefaultClient();
        var url = "/de/area%3Dadmin,controller%3Ddynamic,action%3Dindex";

        // Act
        var response = await client.GetAsync(url);

        // Assert
        await response.AssertStatusCodeAsync(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        Assert.StartsWith("Hello from dynamic controller", content);
    }
}
