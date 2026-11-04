// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net;
using System.Net.Http;
using System.Reflection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.InternalTesting;
using Microsoft.AspNetCore.Mvc.Testing;
using Newtonsoft.Json.Linq;
using Xunit.Abstractions;

namespace Microsoft.AspNetCore.Mvc.FunctionalTests;

// Functional tests for MVC's scenarios with LinkParser
public class LinkParserTest : LoggedTest
{
    private static void ConfigureWebHostBuilder(IWebHostBuilder builder) =>
        builder.UseStartup<RoutingWebSite.StartupForLinkGenerator>();

    protected override void Initialize(TestContext context, MethodInfo methodInfo, object[] testMethodArguments, ITestOutputHelper testOutputHelper)
    {
        base.Initialize(context, methodInfo, testMethodArguments, testOutputHelper);
        Factory = new MvcTestFixture<RoutingWebSite.StartupForLinkGenerator>(LoggerFactory).WithWebHostBuilder(ConfigureWebHostBuilder);
        Client = Factory.CreateDefaultClient();
    }

    public override void Dispose()
    {
        Factory.Dispose();
        base.Dispose();
    }

    public WebApplicationFactory<RoutingWebSite.StartupForLinkGenerator> Factory { get; private set; }
    public HttpClient Client { get; private set; }

    [Fact]
    public async Task ParsePathByEndpoint_CanParsedWithDefaultRoute()
    {
        // Act
        var response = await Client.GetAsync("LinkParser/Index/18");
        var values = await response.Content.ReadAsAsync<JObject>();

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Collection(
            values.Properties().OrderBy(p => p.Name),
            p =>
            {
                Assert.Equal("action", p.Name);
                Assert.Equal("Index", p.Value.Value<string>());
            },
            p =>
            {
                Assert.Equal("controller", p.Name);
                Assert.Equal("LinkParser", p.Value.Value<string>());
            },
            p =>
            {
                Assert.Equal("id", p.Name);
                Assert.Equal("18", p.Value.Value<string>());
            });
    }

    [Fact]
    public async Task ParsePathByEndpoint_CanParsedWithNamedAttributeRoute()
    {
        // Act
        //
        // %2F => /
        var response = await Client.GetAsync("LinkParser/Another?path=%2Fsome-path%2Fa%2Fb%2Fc");
        var values = await response.Content.ReadAsAsync<JObject>();

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Collection(
            values.Properties().OrderBy(p => p.Name),
            p =>
            {
                Assert.Equal("action", p.Name);
                Assert.Equal("AnotherRoute", p.Value.Value<string>());
            },
            p =>
            {
                Assert.Equal("controller", p.Name);
                Assert.Equal("LinkParser", p.Value.Value<string>());
            },
            p =>
            {
                Assert.Equal("x", p.Name);
                Assert.Equal("a", p.Value.Value<string>());
            },
            p =>
            {
                Assert.Equal("y", p.Name);
                Assert.Equal("b", p.Value.Value<string>());
            },
            p =>
            {
                Assert.Equal("z", p.Name);
                Assert.Equal("c", p.Value.Value<string>());
            });
    }
}
