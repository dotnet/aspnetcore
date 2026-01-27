// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.InternalTesting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
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
    public async Task MapControllerRoute_WithStaticAssets_RendersFingerprintedUrls()
    {
        // This test verifies that calling WithStaticAssets() on the builder returned by
        // MapControllerRoute() properly enables fingerprinted URLs in rendered views.
        // This validates the fix for the issue where MapControllerRoute() returned a builder
        // that didn't have the EndpointRouteBuilderKey set in its Items dictionary.

        // Arrange
        var expectedMediaType = MediaTypeHeaderValue.Parse("text/html; charset=utf-8");

        // Act - Render a page that references a static asset
        var response = await Client.GetAsync("HtmlGeneration_Home/StaticAssets");

        // Assert - The request should succeed
        await response.AssertStatusCodeAsync(HttpStatusCode.OK);
        Assert.Equal(expectedMediaType, response.Content.Headers.ContentType);

        // Parse the HTML and find the CSS link
        var document = await response.GetHtmlDocumentAsync();
        var cssLink = document.GetElementById("css-link");
        Assert.NotNull(cssLink);

        // The href should be the fingerprinted URL from the manifest
        // Original URL: ~/styles/site.css
        // Fingerprinted URL: styles/site.fingerprint123.css (based on the label mapping in the manifest)
        var href = cssLink.GetAttribute("href");
        Assert.NotNull(href);
        Assert.Contains("fingerprint123", href);
    }

    [Fact]
    public async Task MapControllerRoute_WithStaticAssets_AddsResourceCollectionToEndpoints()
    {
        // This test verifies that ResourceAssetCollection metadata is properly added to endpoints
        // when using MapControllerRoute().WithStaticAssets()

        // Arrange & Act - Make a request to trigger endpoint resolution
        var response = await Client.GetAsync("HtmlGeneration_Home/Index");

        // Assert - The request should succeed
        await response.AssertStatusCodeAsync(HttpStatusCode.OK);

        // Get endpoints and verify ResourceAssetCollection is present
        var allDataSources = Factory.Services.GetServices<EndpointDataSource>().ToList();
        var allEndpoints = allDataSources.SelectMany(ds => ds.Endpoints).ToList();

        // Find the StaticAssets action endpoint
        var staticAssetsEndpoint = allEndpoints.FirstOrDefault(e =>
            e.DisplayName?.Contains("HtmlGeneration_HomeController") == true &&
            e.DisplayName?.Contains("StaticAssets") == true);

        Assert.NotNull(staticAssetsEndpoint);

        // The endpoint should have ResourceAssetCollection metadata
        var resourceCollection = staticAssetsEndpoint.Metadata.GetMetadata<ResourceAssetCollection>();
        Assert.NotNull(resourceCollection);

        // The resource collection should contain our fingerprinted asset
        var assets = resourceCollection.ToList();
        Assert.NotEmpty(assets);

        // Verify the fingerprinted URL is in the collection
        var fingerprintedAsset = assets.FirstOrDefault(a => a.Url.Contains("fingerprint123"));
        Assert.NotNull(fingerprintedAsset);
    }
}
