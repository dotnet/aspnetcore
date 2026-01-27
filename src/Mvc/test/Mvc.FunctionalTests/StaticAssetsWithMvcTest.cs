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
    public async Task MapControllerRoute_WithStaticAssets_ApplicationStarts()
    {
        // This test verifies that calling WithStaticAssets() on the builder returned by
        // MapControllerRoute() doesn't cause errors during application startup.
        // This validates the fix for the issue where MapControllerRoute() returned a builder
        // that didn't have the EndpointRouteBuilderKey set in its Items dictionary.
        //
        // Note: The full static assets flow requires a proper manifest file generated at build time.
        // The unit tests in ControllerActionEndpointConventionBuilderResourceCollectionExtensionsTest
        // verify that WithStaticAssets() properly adds ResourceAssetCollection metadata to endpoints.

        // Arrange & Act - Make a request to verify the application started successfully
        var response = await Client.GetAsync("HtmlGeneration_Home/Index");

        // Assert - The request should succeed, proving that WithStaticAssets() didn't throw
        await response.AssertStatusCodeAsync(HttpStatusCode.OK);

        // Verify we can access endpoints (this would fail if routing setup failed)
        var allDataSources = Factory.Services.GetServices<EndpointDataSource>().ToList();
        var allEndpoints = allDataSources.SelectMany(ds => ds.Endpoints).ToList();

        // Find the Index endpoint from HtmlGeneration_HomeController
        var indexEndpoint = allEndpoints.FirstOrDefault(e =>
            e.DisplayName?.Contains("HtmlGeneration_HomeController") == true &&
            e.DisplayName?.Contains("Index") == true);

        Assert.NotNull(indexEndpoint);
    }
}
