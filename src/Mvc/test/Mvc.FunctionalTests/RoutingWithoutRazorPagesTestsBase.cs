// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net;
using System.Net.Http;
using System.Reflection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.InternalTesting;
using Microsoft.AspNetCore.Mvc.Testing;
using Newtonsoft.Json;
using Xunit.Abstractions;

namespace Microsoft.AspNetCore.Mvc.FunctionalTests;

public abstract class RoutingWithoutRazorPagesTestsBase<TStartup> : LoggedTest where TStartup : class
{
    private static void ConfigureWebHostBuilder(IWebHostBuilder builder) =>
        builder.UseStartup<TStartup>();

    protected override void Initialize(TestContext context, MethodInfo methodInfo, object[] testMethodArguments, ITestOutputHelper testOutputHelper)
    {
        base.Initialize(context, methodInfo, testMethodArguments, testOutputHelper);
        Factory = new MvcTestFixture<TStartup>(LoggerFactory).WithWebHostBuilder(ConfigureWebHostBuilder);
        Client = Factory.CreateDefaultClient();
    }

    public override void Dispose()
    {
        Factory.Dispose();
        base.Dispose();
    }

    public WebApplicationFactory<TStartup> Factory { get; private set; }
    public HttpClient Client { get; private set; }

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
