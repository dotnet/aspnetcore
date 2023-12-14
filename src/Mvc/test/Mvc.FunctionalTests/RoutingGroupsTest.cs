// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using RoutingWebSite;

namespace Microsoft.AspNetCore.Mvc.FunctionalTests;

public class RoutingGroupsTests : IClassFixture<MvcTestFixture<StartupForGroups>>
{
    public RoutingGroupsTests(MvcTestFixture<StartupForGroups> fixture)
    {
        Factory = fixture.Factories.FirstOrDefault() ?? fixture.WithWebHostBuilder(ConfigureWebHostBuilder);
    }

    private static void ConfigureWebHostBuilder(IWebHostBuilder builder) => builder.UseStartup<StartupForGroups>();

    public WebApplicationFactory<StartupForGroups> Factory { get; }

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
