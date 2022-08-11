// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using RoutingWebSite;

namespace Microsoft.AspNetCore.Mvc.FunctionalTests;

public class RoutingGroupsWithMetadataTests : IClassFixture<MvcTestFixture<StartupForRouteGroupsWithMetadata>>
{
    public RoutingGroupsWithMetadataTests(MvcTestFixture<StartupForRouteGroupsWithMetadata> fixture)
    {
        Factory = fixture.Factories.FirstOrDefault() ?? fixture.WithWebHostBuilder(ConfigureWebHostBuilder);
    }

    private static void ConfigureWebHostBuilder(IWebHostBuilder builder) => builder.UseStartup<StartupForRouteGroupsWithMetadata>();

    public WebApplicationFactory<StartupForRouteGroupsWithMetadata> Factory { get; }

    [Fact]
    public async Task OrderedGroupMetadataForControllers()
    {
        using var client = Factory.CreateClient();

        var response = await client.GetAsync("group1/metadata");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var content = await response.Content.ReadFromJsonAsync<string[]>();

        Assert.Equal(new[] { "A", "C", "B" }, content);
    }
}
