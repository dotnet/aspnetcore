// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Reflection;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.InternalTesting;
using Microsoft.AspNetCore.Mvc.Testing;
using RoutingWebSite;
using Xunit.Abstractions;

namespace Microsoft.AspNetCore.Mvc.FunctionalTests;

public class ControllerEndpointFiltersTest : LoggedTest
{
    protected override void Initialize(TestContext context, MethodInfo methodInfo, object[] testMethodArguments, ITestOutputHelper testOutputHelper)
    {
        base.Initialize(context, methodInfo, testMethodArguments, testOutputHelper);
        Factory = new MvcTestFixture<StartupForEndpointFilters>(LoggerFactory).WithWebHostBuilder(ConfigureWebHostBuilder);
    }

    public override void Dispose()
    {
        Factory.Dispose();
        base.Dispose();
    }

    private static void ConfigureWebHostBuilder(IWebHostBuilder builder) => builder.UseStartup<StartupForEndpointFilters>();

    public WebApplicationFactory<StartupForEndpointFilters> Factory { get; private set; }

    [Fact]
    public async Task CanApplyEndpointFilterToController()
    {
        using var client = Factory.CreateClient();

        var response = await client.GetAsync("Items/Index");
        var content = await response.Content.ReadFromJsonAsync<Dictionary<string, object>>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True(content.TryGetValue(nameof(IEndpointFilter), out var endpointFilterCalled));
        Assert.True(((JsonElement)endpointFilterCalled).GetBoolean());
    }

    [Fact]
    public async Task CanCaptureMethodInfoFromControllerAction()
    {
        using var client = Factory.CreateClient();

        var response = await client.GetAsync("Items/Index");
        var content = await response.Content.ReadFromJsonAsync<Dictionary<string, object>>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True(content.TryGetValue(nameof(EndpointFilterFactoryContext.MethodInfo.Name), out var methodInfo));
        Assert.Equal("Index", ((JsonElement)methodInfo).GetString());
    }

    [Fact]
    public async Task CanInterceptActionResultViaFilter()
    {
        using var client = Factory.CreateClient();

        var response = await client.GetAsync("Items/IndexWithSelectiveFilter");
        var content = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("Intercepted", content);
    }

    [Fact]
    public async Task CanAccessArgumentsFromAction()
    {
        using var client = Factory.CreateClient();

        var response = await client.GetAsync("Items/IndexWithArgument/foobar");
        var content = await response.Content.ReadFromJsonAsync<Dictionary<string, object>>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True(content.TryGetValue(nameof(EndpointFilterInvocationContext.Arguments), out var argument));
        Assert.Equal("foobar", ((JsonElement)argument).GetString());
    }
}
