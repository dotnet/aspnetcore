// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net;
using System.Net.Http;
using System.Reflection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.InternalTesting;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit.Abstractions;

namespace Microsoft.AspNetCore.Mvc.FunctionalTests;

/// <summary>
/// Functional tests that verify static assets work correctly with MapControllerRoute and WithStaticAssets.
/// </summary>
public class StaticAssetsWithMvcTest : LoggedTest
{
    protected override void Initialize(TestContext context, MethodInfo methodInfo, object[] testMethodArguments, ITestOutputHelper testOutputHelper)
    {
        base.Initialize(context, methodInfo, testMethodArguments, testOutputHelper);
        Factory = new MvcTestFixture<HtmlGenerationWebSite.StartupWithStaticAssets>(LoggerFactory)
            .WithWebHostBuilder(ConfigureWebHostBuilder);
        Client = Factory.CreateDefaultClient();
    }

    public override void Dispose()
    {
        Factory.Dispose();
        base.Dispose();
    }

    public WebApplicationFactory<HtmlGenerationWebSite.StartupWithStaticAssets> Factory { get; private set; }
    public HttpClient Client { get; private set; }

    private static void ConfigureWebHostBuilder(IWebHostBuilder builder) =>
        builder.UseStartup<HtmlGenerationWebSite.StartupWithStaticAssets>();

    [Fact]
    public async Task StaticAssets_WithMapControllerRoute_RendersFingerprintedUrl()
    {
        // Arrange & Act
        var response = await Client.GetAsync("http://localhost/HtmlGeneration_Home/StaticAssets");

        // Assert
        await response.AssertStatusCodeAsync(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();

        // The CSS link should have a fingerprinted URL
        // The manifest maps styles/site.css -> styles/site.fingerprint123.css
        Assert.Contains("styles/site.fingerprint123.css", content);
        Assert.DoesNotContain("href=\"/styles/site.css\"", content);
    }
}
