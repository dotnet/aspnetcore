// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Text;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.InternalTesting;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit.Abstractions;

namespace Microsoft.AspNetCore.Mvc.FunctionalTests;

// Verifies that C# union types flow through the Razor Pages handler pipeline. The
// underlying JSON formatters are the same SystemTextJsonInputFormatter /
// SystemTextJsonOutputFormatter that MVC controllers use — this test class exists as a
// smoke test that confirms Razor Pages model binding for [FromBody] union parameters
// and IActionResult JsonResult responses behave the same way for unions.
public class RazorPagesUnionsTest : LoggedTest
{
    private static void ConfigureWebHostBuilder(IWebHostBuilder builder) =>
        builder.UseStartup<RazorPagesWebSite.Startup>();

    protected override void Initialize(TestContext context, MethodInfo methodInfo, object[] testMethodArguments, ITestOutputHelper testOutputHelper)
    {
        base.Initialize(context, methodInfo, testMethodArguments, testOutputHelper);
        Factory = new MvcTestFixture<RazorPagesWebSite.Startup>(LoggerFactory).WithWebHostBuilder(ConfigureWebHostBuilder);
        Client = Factory.CreateDefaultClient();
    }

    public override void Dispose()
    {
        Factory.Dispose();
        base.Dispose();
    }

    public WebApplicationFactory<RazorPagesWebSite.Startup> Factory { get; private set; }
    public HttpClient Client { get; private set; }

    [Fact]
    public async Task RazorPage_UnionReturnType_SerializesActiveCase()
    {
        var response = await Client.GetAsync("/Unions");

        await response.AssertStatusCodeAsync(HttpStatusCode.OK);
        Assert.Equal("true", await response.Content.ReadAsStringAsync());
    }

    [Theory]
    [InlineData("true", "true")]
    [InlineData("false", "false")]
    [InlineData("\"hi\"", "\"hi\"")]
    public async Task RazorPage_UnionFromBody_RoundTripsUnambiguousPrimitiveCases(string payload, string expectedBody)
    {
        var content = new StringContent(payload, Encoding.UTF8, "application/json");

        var response = await Client.PostAsync("/Unions", content);

        await response.AssertStatusCodeAsync(HttpStatusCode.OK);
        Assert.Equal(expectedBody, await response.Content.ReadAsStringAsync());
    }
}
