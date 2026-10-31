// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net;
using System.Reflection;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.InternalTesting;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit.Abstractions;

namespace Microsoft.AspNetCore.Mvc.FunctionalTests;

/// <summary>
/// Functional tests that verify static assets work correctly with conventional controller routes.
/// </summary>
public class StaticAssetsWithMvcTest : LoggedTest
{
    protected override void Initialize(TestContext context, MethodInfo methodInfo, object[] testMethodArguments, ITestOutputHelper testOutputHelper)
    {
        base.Initialize(context, methodInfo, testMethodArguments, testOutputHelper);
        Factory = new MvcTestFixture<HtmlGenerationWebSite.StartupWithStaticAssets>(LoggerFactory)
            .WithWebHostBuilder(ConfigureWebHostBuilder);
    }

    public override void Dispose()
    {
        Factory.Dispose();
        base.Dispose();
    }

    public WebApplicationFactory<HtmlGenerationWebSite.StartupWithStaticAssets> Factory { get; private set; }

    private static void ConfigureWebHostBuilder(IWebHostBuilder builder) =>
        builder.UseStartup<HtmlGenerationWebSite.StartupWithStaticAssets>();

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task StaticAssets_WithConventionalRoute_RendersFingerprintedUrlsAndImportMap(bool useDefaultControllerRoute)
    {
        using var factory = Factory.WithWebHostBuilder(builder =>
            builder.UseSetting("UseDefaultControllerRoute", useDefaultControllerRoute.ToString()));
        using var client = factory.CreateDefaultClient();
        using var response = await client.GetAsync("http://localhost/HtmlGeneration_Home/StaticAssets");

        await response.AssertStatusCodeAsync(HttpStatusCode.OK);
        var document = await response.GetHtmlDocumentAsync();

        Assert.Equal("/styles/site.fingerprint123.css", document.RequiredQuerySelector("#test-css").GetAttribute("href"));
        Assert.Equal("/styles/site.fingerprint123.js", document.RequiredQuerySelector("#test-js").GetAttribute("src"));

        var importMap = document.RequiredQuerySelector("head script[type=importmap]");
        using var json = JsonDocument.Parse(importMap.TextContent);
        Assert.Equal("./styles/site.fingerprint123.js",
            json.RootElement.GetProperty("imports").GetProperty("./styles/site.js").GetString());
    }
}
