// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.InternalTesting;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit.Abstractions;

namespace Microsoft.AspNetCore.Mvc.FunctionalTests;

public class StaticAssetsWithMvcTest : LoggedTest
{
    protected override void Initialize(TestContext context, MethodInfo methodInfo, object[] testMethodArguments, ITestOutputHelper testOutputHelper)
    {
        base.Initialize(context, methodInfo, testMethodArguments, testOutputHelper);
        Factory = new MvcTestFixture<HtmlGenerationWebSite.StartupWithStaticAssets>(LoggerFactory);
        Client = Factory.CreateDefaultClient();
    }

    public override void Dispose()
    {
        Factory.Dispose();
        base.Dispose();
    }

    public WebApplicationFactory<HtmlGenerationWebSite.StartupWithStaticAssets> Factory { get; private set; }
    public HttpClient Client { get; private set; }

    [Fact]
    public async Task MapControllerRoute_WithStaticAssets_RendersFingerprintedCssUrls()
    {
        // Arrange
        var expectedMediaType = MediaTypeHeaderValue.Parse("text/html; charset=utf-8");

        // Act
        var response = await Client.GetAsync("HtmlGeneration_Home/StaticAssets");

        // Assert
        await response.AssertStatusCodeAsync(HttpStatusCode.OK);
        Assert.Equal(expectedMediaType, response.Content.Headers.ContentType);
        var document = await response.GetHtmlDocumentAsync();

        // The link tag should have a fingerprinted URL
        var cssLink = document.GetElementById("css-link");
        Assert.NotNull(cssLink);
        Assert.Equal("link", cssLink.TagName, ignoreCase: true);

        var href = cssLink.GetAttribute("href");
        Assert.NotNull(href);

        // The href should contain the fingerprinted version (styles/site.fingerprint123.css)
        // instead of the original (~/styles/site.css)
        Assert.Contains("fingerprint123", href);
        Assert.Contains("styles/site", href);
        Assert.EndsWith(".css", href);
    }
}
