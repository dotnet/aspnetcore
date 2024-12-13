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

public class RoutingAcrossPipelineBranchesTests : LoggedTest
{
    private static void ConfigureWebHostBuilder(IWebHostBuilder builder) => builder.UseStartup<RoutingWebSite.StartupRoutingDifferentBranches>();

    protected override void Initialize(TestContext context, MethodInfo methodInfo, object[] testMethodArguments, ITestOutputHelper testOutputHelper)
    {
        base.Initialize(context, methodInfo, testMethodArguments, testOutputHelper);
        Factory = new MvcTestFixture<RoutingWebSite.StartupRoutingDifferentBranches>(LoggerFactory).WithWebHostBuilder(ConfigureWebHostBuilder);
        Client = Factory.CreateDefaultClient();
    }

    public override void Dispose()
    {
        Factory.Dispose();
        base.Dispose();
    }

    public WebApplicationFactory<RoutingWebSite.StartupRoutingDifferentBranches> Factory { get; private set; }
    public HttpClient Client { get; private set; }

    [Fact]
    public async Task MatchesConventionalRoutesInTheirBranches()
    {
        var client = Factory.CreateClient();

        // Arrange
        var subdirRequest = new HttpRequestMessage(HttpMethod.Get, "subdir/literal/Branches/Index/s");
        var commonRequest = new HttpRequestMessage(HttpMethod.Get, "common/Branches/Index/c/literal");
        var defaultRequest = new HttpRequestMessage(HttpMethod.Get, "Branches/literal/Index/d");

        // Act
        var subdirResponse = await client.SendAsync(subdirRequest);
        var subdirContent = await subdirResponse.Content.ReadFromJsonAsync<RouteInfo>();

        var commonResponse = await client.SendAsync(commonRequest);
        var commonContent = await commonResponse.Content.ReadFromJsonAsync<RouteInfo>();

        var defaultResponse = await client.SendAsync(defaultRequest);
        var defaultContent = await defaultResponse.Content.ReadFromJsonAsync<RouteInfo>();

        // Assert
        Assert.Equal(HttpStatusCode.OK, subdirResponse.StatusCode);
        Assert.True(subdirContent.RouteValues.TryGetValue("subdir", out var subdir));
        Assert.Equal("s", subdir);

        Assert.Equal(HttpStatusCode.OK, commonResponse.StatusCode);
        Assert.True(commonContent.RouteValues.TryGetValue("common", out var common));
        Assert.Equal("c", common);

        Assert.Equal(HttpStatusCode.OK, defaultResponse.StatusCode);
        Assert.True(defaultContent.RouteValues.TryGetValue("default", out var @default));
        Assert.Equal("d", @default);
    }

    [Fact]
    public async Task LinkGenerationWorksOnEachBranch()
    {
        var client = Factory.CreateClient();
        var linkQuery = "?link";

        // Arrange
        var subdirRequest = new HttpRequestMessage(HttpMethod.Get, "subdir/literal/Branches/Index/s" + linkQuery);
        var commonRequest = new HttpRequestMessage(HttpMethod.Get, "common/Branches/Index/c/literal" + linkQuery);
        var defaultRequest = new HttpRequestMessage(HttpMethod.Get, "Branches/literal/Index/d" + linkQuery);

        // Act
        var subdirResponse = await client.SendAsync(subdirRequest);
        var subdirContent = await subdirResponse.Content.ReadFromJsonAsync<RouteInfo>();

        var commonResponse = await client.SendAsync(commonRequest);
        var commonContent = await commonResponse.Content.ReadFromJsonAsync<RouteInfo>();

        var defaultResponse = await client.SendAsync(defaultRequest);
        var defaultContent = await defaultResponse.Content.ReadFromJsonAsync<RouteInfo>();

        // Assert
        Assert.Equal(HttpStatusCode.OK, subdirResponse.StatusCode);
        Assert.Equal("/subdir/literal/Branches/Index/s", subdirContent.Link);

        Assert.Equal(HttpStatusCode.OK, commonResponse.StatusCode);
        Assert.Equal("/common/Branches/Index/c/literal", commonContent.Link);

        Assert.Equal(HttpStatusCode.OK, defaultResponse.StatusCode);
        Assert.Equal("/Branches/literal/Index/d", defaultContent.Link);
    }

    // This still works because even though each middleware now gets its own data source,
    // those data sources still get added to a global collection in IOptions<RouteOptions>>.DataSources
    [Fact]
    public async Task LinkGenerationStillWorksAcrossBranches()
    {
        var client = Factory.CreateClient();
        var linkQuery = "?link";

        // Arrange
        var subdirRequest = new HttpRequestMessage(HttpMethod.Get, "subdir/literal/Branches/Index/s" + linkQuery + "&link_common=c&link_subdir");
        var defaultRequest = new HttpRequestMessage(HttpMethod.Get, "Branches/literal/Index/d" + linkQuery + "&link_subdir=s");

        // Act
        var subdirResponse = await client.SendAsync(subdirRequest);
        var subdirContent = await subdirResponse.Content.ReadFromJsonAsync<RouteInfo>();

        var defaultResponse = await client.SendAsync(defaultRequest);
        var defaultContent = await defaultResponse.Content.ReadFromJsonAsync<RouteInfo>();

        // Assert
        Assert.Equal(HttpStatusCode.OK, subdirResponse.StatusCode);
        // Note that this link and the one below don't account for the path base being in a different branch.
        // The recommendation for customers doing link generation across branches will be to always generate absolute
        // URIs by explicitly passing the path base to the link generator.
        // In the future there are improvements we might be able to do in most common cases to lift this limitation if we receive
        // feedback about it.
        Assert.Equal("/subdir/Branches/Index/c/literal", subdirContent.Link);

        Assert.Equal(HttpStatusCode.OK, defaultResponse.StatusCode);
        Assert.Equal("/literal/Branches/Index/s", defaultContent.Link);
    }

    [Fact]
    public async Task DoesNotMatchConventionalRoutesDefinedInOtherBranches()
    {
        var client = Factory.CreateClient();

        // Arrange
        var commonRequest = new HttpRequestMessage(HttpMethod.Get, "common/literal/Branches/Index/s");
        var subdirRequest = new HttpRequestMessage(HttpMethod.Get, "subdir/Branches/Index/c/literal");
        var defaultRequest = new HttpRequestMessage(HttpMethod.Get, "common/Branches/literal/Index/d");

        // Act
        var commonResponse = await client.SendAsync(commonRequest);

        var subdirResponse = await client.SendAsync(subdirRequest);

        var defaultResponse = await client.SendAsync(defaultRequest);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, commonResponse.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, subdirResponse.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, defaultResponse.StatusCode);
    }

    [Fact]
    public async Task ConventionalAndDynamicRouteOrdersAreScopedPerBranch()
    {
        var client = Factory.CreateClient();

        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Get, "dynamicattributeorder/dynamic/route/rest");

        // Act
        var response = await client.SendAsync(request);
        var content = await response.Content.ReadFromJsonAsync<RouteInfo>();

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True(content.RouteValues.TryGetValue("action", out var action));

        // The dynamic route wins because it has Order 1 (scope to that router) and
        // has higher precedence.
        Assert.Equal("Index", action);
    }

    private record RouteInfo(string RouteName, IDictionary<string, string> RouteValues, string Link);
}
