// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net;
using System.Net.Http.Json;
using System.Reflection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.InternalTesting;
using Microsoft.AspNetCore.Mvc.Testing;
using RoutingWebSite;
using Xunit.Abstractions;

namespace Microsoft.AspNetCore.Mvc.FunctionalTests;

public class RoutingGroupsWithMetadataTests : LoggedTest
{
    private static void ConfigureWebHostBuilder(IWebHostBuilder builder) => builder.UseStartup<StartupForRouteGroupsWithMetadata>();

    protected override void Initialize(TestContext context, MethodInfo methodInfo, object[] testMethodArguments, ITestOutputHelper testOutputHelper)
    {
        base.Initialize(context, methodInfo, testMethodArguments, testOutputHelper);
        Factory = new MvcTestFixture<StartupForRouteGroupsWithMetadata>(LoggerFactory).WithWebHostBuilder(ConfigureWebHostBuilder);
    }

    public override void Dispose()
    {
        Factory.Dispose();
        base.Dispose();
    }

    public WebApplicationFactory<StartupForRouteGroupsWithMetadata> Factory { get; private set; }

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
