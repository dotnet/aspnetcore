// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Reflection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.InternalTesting;
using Microsoft.AspNetCore.Mvc.Testing;
using RoutingWebSite;
using Xunit.Abstractions;

namespace Microsoft.AspNetCore.Mvc.FunctionalTests;

public class RoutingGroupsTests : LoggedTest
{
    private static void ConfigureWebHostBuilder(IWebHostBuilder builder) => builder.UseStartup<StartupForGroups>();

    protected override void Initialize(TestContext context, MethodInfo methodInfo, object[] testMethodArguments, ITestOutputHelper testOutputHelper)
    {
        base.Initialize(context, methodInfo, testMethodArguments, testOutputHelper);
        Factory = new MvcTestFixture<StartupForGroups>(LoggerFactory).WithWebHostBuilder(ConfigureWebHostBuilder);
        Client = Factory.CreateDefaultClient();
    }

    public override void Dispose()
    {
        Factory.Dispose();
        base.Dispose();
    }

    public WebApplicationFactory<StartupForGroups> Factory { get; private set; }
    public HttpClient Client { get; private set; }

    [Fact]
    public async Task MatchesControllerGroup()
    {
        using var client = Factory.CreateClient();

        var response = await client.GetAsync("controllers/contoso/Blog/ShowPosts");
        var content = await response.Content.ReadFromJsonAsync<RouteInfo>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True(content.RouteValues.TryGetValue("org", out var org));
        Assert.Equal("contoso", org);
    }

    [Fact]
    public async Task MatchesPagesGroupAndGeneratesValidLink()
    {
        using var client = Factory.CreateClient();

        var response = await client.GetAsync("pages/PageWithLinks");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var document = await response.GetHtmlDocumentAsync();
        var editLink = document.RequiredQuerySelector("#editlink");
        var contactLink = document.RequiredQuerySelector("#contactlink");
        Assert.Equal("/pages/Edit/10", editLink.GetAttribute("href"));
        Assert.Equal("/controllers/contoso/Home/Contact", contactLink.GetAttribute("href"));
    }

    private record RouteInfo(string RouteName, IDictionary<string, string> RouteValues, string Link);
}
