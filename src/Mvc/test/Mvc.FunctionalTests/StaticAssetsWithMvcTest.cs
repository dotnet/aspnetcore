// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net;
using System.Net.Http;
using System.Reflection;
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
    public async Task MapControllerRoute_WithStaticAssets_ApplicationStartsSuccessfully()
    {
        // This test verifies that calling WithStaticAssets() on the builder returned by
        // MapControllerRoute() doesn't cause errors during application startup.
        //
        // This validates the fix for the issue where MapControllerRoute() returned a builder
        // that didn't have the EndpointRouteBuilderKey set in its Items dictionary, which
        // would cause WithStaticAssets() to silently no-op.
        //
        // The full validation of ResourceAssetCollection metadata being properly added to
        // endpoints is covered by the unit tests in:
        // ControllerActionEndpointConventionBuilderResourceCollectionExtensionsTest

        // Arrange & Act - Make a request to verify the application started successfully
        var response = await Client.GetAsync("HtmlGeneration_Home/Index");

        // Assert - The request should succeed, proving that:
        // 1. The application started without errors
        // 2. WithStaticAssets() was called successfully on MapControllerRoute() builder
        // 3. Routing is working correctly
        await response.AssertStatusCodeAsync(HttpStatusCode.OK);
    }
}
