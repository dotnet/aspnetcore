// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components.E2ETest.Infrastructure.ServerFixtures;
using Microsoft.AspNetCore.Components.E2ETest.Infrastructure;
using Microsoft.AspNetCore.E2ETesting;
using TestServer;
using Xunit.Abstractions;
using Components.TestServer.RazorComponents;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.Extensions;

namespace Microsoft.AspNetCore.Components.E2ETests.ServerRenderingTests;

[CollectionDefinition(nameof(EnhancedNavigationTest), DisableParallelization = true)]
public class EnhancedNavigationTest : ServerTestBase<BasicTestAppServerSiteFixture<RazorComponentEndpointsStartup<App>>>
{
    public EnhancedNavigationTest(
        BrowserFixture browserFixture,
        BasicTestAppServerSiteFixture<RazorComponentEndpointsStartup<App>> serverFixture,
        ITestOutputHelper output)
        : base(browserFixture, serverFixture, output)
    {
    }

    // One of the tests here makes use of the streaming rendering page, which uses global state
    // so we can't run at the same time as other such tests
    public override Task InitializeAsync()
        => InitializeAsync(BrowserFixture.StreamingContext);

    [Fact]
    public void CanNavigateToAnotherPageWhilePreservingCommonDOMElements()
    {
        Navigate($"{ServerPathBase}/nav");

        var h1Elem = Browser.Exists(By.TagName("h1"));
        Browser.Equal("Hello", () => h1Elem.Text);

        Browser.Exists(By.TagName("nav")).FindElement(By.LinkText("Streaming")).Click();

        // Important: we're checking the *same* <h1> element as earlier, showing that we got to the
        // destination, and it's done so without a page load, and it preserved the element
        Browser.Equal("Streaming Rendering", () => h1Elem.Text);

        // We have to make the response finish otherwise the test will fail when it tries to dispose the server
        Browser.FindElement(By.Id("end-response-link")).Click();
    }

    [Fact]
    public void CanNavigateToAnHtmlPageWithAnErrorStatus()
    {
        Navigate($"{ServerPathBase}/nav");
        Browser.Exists(By.TagName("nav")).FindElement(By.LinkText("Error page with 404 content")).Click();
        Browser.Equal("404", () => Browser.Exists(By.TagName("h1")).Text);
    }

    [Fact]
    public void DisplaysStatusCodeIfResponseIsErrorWithNoContent()
    {
        Navigate($"{ServerPathBase}/nav");
        Browser.Exists(By.TagName("nav")).FindElement(By.LinkText("Error page with no content")).Click();
        Browser.Equal("Error: 404 Not Found", () => Browser.Exists(By.TagName("html")).Text);
    }

    [Fact]
    public void CanNavigateToNonHtmlResponse()
    {
        Navigate($"{ServerPathBase}/nav");
        Browser.Exists(By.TagName("nav")).FindElement(By.LinkText("Non-HTML page")).Click();
        Browser.Equal("Hello, this is plain text", () => Browser.Exists(By.TagName("html")).Text);
    }

    [Fact]
    public void EnhancedNavRequestsIncludeExpectedHeaders()
    {
        Navigate($"{ServerPathBase}/nav");
        Browser.Exists(By.TagName("nav")).FindElement(By.LinkText("List headers")).Click();

        var ul = Browser.Exists(By.Id("all-headers"));
        var allHeaders = ul.FindElements(By.TagName("li")).Select(x => x.Text.ToLowerInvariant()).ToList();

        // Specifying text/html is to make the enhanced nav outcomes more similar to non-enhanced nav.
        // For example, the default error middleware will only serve the error page if this content type is requested.
        // The blazor-enhanced-nav parameter can be used to trigger arbitrary server-side behaviors.
        Assert.Contains("accept: text/html; blazor-enhanced-nav=on", allHeaders);
    }

    [Fact]
    public void EnhancedNavCanBeDisabledHierarchically()
    {
        Navigate($"{ServerPathBase}/nav");

        var originalH1Elem = Browser.Exists(By.TagName("h1"));
        Browser.Equal("Hello", () => originalH1Elem.Text);

        Browser.Exists(By.TagName("nav")).FindElement(By.Id("not-enhanced-nav-link")).Click();

        // Check we got there, but we did *not* retain the <h1> element
        Browser.Equal("Other", () => Browser.Exists(By.TagName("h1")).Text);
        Assert.Throws<StaleElementReferenceException>(() => originalH1Elem.Text);
    }

    [Fact]
    public void EnhancedNavCanBeReenabledHierarchically()
    {
        Navigate($"{ServerPathBase}/nav");

        var originalH1Elem = Browser.Exists(By.TagName("h1"));
        Browser.Equal("Hello", () => originalH1Elem.Text);

        Browser.Exists(By.TagName("nav")).FindElement(By.LinkText("Other (re-enabled enhanced nav)")).Click();

        // Check we got there, and it did retain the <h1> element
        Browser.Equal("Other", () => Browser.Exists(By.TagName("h1")).Text);
        Assert.Equal("Other", originalH1Elem.Text);
    }

    [Fact]
    public void EnhancedNavWorksInsideSVGElement()
    {
        Navigate($"{ServerPathBase}/nav");

        var originalH1Elem = Browser.Exists(By.TagName("h1"));
        Browser.Equal("Hello", () => originalH1Elem.Text);

        Browser.Exists(By.TagName("nav")).FindElement(By.Id("svg-nav-link")).Click();

        // Check we got there, and it did retain the <h1> element
        Browser.Equal("Other", () => Browser.Exists(By.TagName("h1")).Text);
        Assert.Equal("Other", originalH1Elem.Text);
    }

    [Fact]
    public void EnhancedNavCanBeDisabledInSVGElementContainingAnchor()
    {
        Navigate($"{ServerPathBase}/nav");

        var originalH1Elem = Browser.Exists(By.TagName("h1"));
        Browser.Equal("Hello", () => originalH1Elem.Text);

        Browser.Exists(By.TagName("nav")).FindElement(By.Id("svg-not-enhanced-nav-link")).Click();

        // Check we got there, but we did *not* retain the <h1> element
        Browser.Equal("Other", () => Browser.Exists(By.TagName("h1")).Text);
        Assert.Throws<StaleElementReferenceException>(() => originalH1Elem.Text);
    }

    [Fact]
    public void EnhancedNavCanBeDisabledInSVGElementInsideAnchor()
    {
        Navigate($"{ServerPathBase}/nav");

        var originalH1Elem = Browser.Exists(By.TagName("h1"));
        Browser.Equal("Hello", () => originalH1Elem.Text);

        Browser.Exists(By.TagName("nav")).FindElement(By.Id("svg-in-anchor-not-enhanced-nav-link")).Click();

        // Check we got there, but we did *not* retain the <h1> element
        Browser.Equal("Other", () => Browser.Exists(By.TagName("h1")).Text);
        Assert.Throws<StaleElementReferenceException>(() => originalH1Elem.Text);
    }

    [Fact]
    public void ScrollsToHashWithContentAddedAsynchronously()
    {
        Navigate($"{ServerPathBase}/nav");
        Browser.Exists(By.TagName("nav")).FindElement(By.LinkText("Scroll to hash")).Click();
        Assert.Equal(0, Browser.GetScrollY());

        var asyncContentHeader = Browser.Exists(By.Id("some-content"));
        Browser.Equal("Some content", () => asyncContentHeader.Text);
        Browser.True(() => Browser.GetScrollY() > 500);
    }

    [Fact]
    public void CanScrollToHashWithoutPerformingFullNavigation()
    {
        Navigate($"{ServerPathBase}/nav/scroll-to-hash");
        Browser.Equal("Scroll to hash", () => Browser.Exists(By.TagName("h1")).Text);

        Browser.Exists(By.Id("scroll-anchor")).Click();
        Browser.True(() => Browser.GetScrollY() > 500);
        Browser.True(() => Browser
            .Exists(By.Id("uri-on-page-load"))
            .GetDomAttribute("data-value")
            .EndsWith("scroll-to-hash", StringComparison.Ordinal));
    }

    [Theory]
    [InlineData("server")]
    [InlineData("webassembly")]
    public void CanPerformProgrammaticEnhancedNavigation(string renderMode)
    {
        Navigate($"{ServerPathBase}/nav");
        Browser.Equal("Hello", () => Browser.Exists(By.TagName("h1")).Text);

        // Normally, you shouldn't store references to elements because they could become stale references
        // after the page re-renders. However, we want to explicitly test that the element persists across
        // renders to ensure that enhanced navigation occurs instead of a full page reload.
        // Here, we pick an element that we know will persist across navigations so we can check
        // for its staleness.
        var elementForStalenessCheck = Browser.Exists(By.TagName("html"));

        Browser.Exists(By.TagName("nav")).FindElement(By.LinkText($"Interactive component navigation ({renderMode})")).Click();
        Browser.Equal("Page with interactive components that navigate", () => Browser.Exists(By.TagName("h1")).Text);
        Browser.False(() => IsElementStale(elementForStalenessCheck));

        Browser.Exists(By.Id("navigate-to-another-page")).Click();
        Browser.Equal("Hello", () => Browser.Exists(By.TagName("h1")).Text);
        Assert.EndsWith("/nav", Browser.Url);
        Browser.False(() => IsElementStale(elementForStalenessCheck));

        // Ensure that the history stack was correctly updated
        Browser.Navigate().Back();
        Browser.Equal("Page with interactive components that navigate", () => Browser.Exists(By.TagName("h1")).Text);
        Browser.False(() => IsElementStale(elementForStalenessCheck));

        Browser.Navigate().Back();
        Browser.Equal("Hello", () => Browser.Exists(By.TagName("h1")).Text);
        Assert.EndsWith("/nav", Browser.Url);
        Browser.False(() => IsElementStale(elementForStalenessCheck));
    }

    [Theory]
    [InlineData("server", "refresh-with-navigate-to")]
    [InlineData("webassembly", "refresh-with-navigate-to")]
    [InlineData("server", "refresh-with-refresh")]
    [InlineData("webassembly", "refresh-with-refresh")]
    public void CanPerformProgrammaticEnhancedRefresh(string renderMode, string refreshButtonId)
    {
        Navigate($"{ServerPathBase}/nav");
        Browser.Equal("Hello", () => Browser.Exists(By.TagName("h1")).Text);

        Browser.Exists(By.TagName("nav")).FindElement(By.LinkText($"Interactive component navigation ({renderMode})")).Click();
        Browser.Equal("Page with interactive components that navigate", () => Browser.Exists(By.TagName("h1")).Text);

        // Normally, you shouldn't store references to elements because they could become stale references
        // after the page re-renders. However, we want to explicitly test that the element persists across
        // renders to ensure that enhanced navigation occurs instead of a full page reload.
        var renderIdElement = Browser.Exists(By.Id("render-id"));
        var initialRenderId = -1;
        Browser.True(() => int.TryParse(renderIdElement.Text, out initialRenderId));
        Assert.NotEqual(-1, initialRenderId);

        Browser.Exists(By.Id(refreshButtonId)).Click();
        Browser.True(() =>
        {
            if (IsElementStale(renderIdElement) || !int.TryParse(renderIdElement.Text, out var newRenderId))
            {
                return false;
            }

            return newRenderId > initialRenderId;
        });

        // Ensure that the history stack was correctly updated
        Browser.Navigate().Back();
        Browser.Equal("Hello", () => Browser.Exists(By.TagName("h1")).Text);
        Assert.EndsWith("/nav", Browser.Url);
    }

    [Theory]
    [InlineData("server")]
    [InlineData("webassembly")]
    public void NavigateToCanFallBackOnFullPageReload(string renderMode)
    {
        Navigate($"{ServerPathBase}/nav");
        Browser.Equal("Hello", () => Browser.Exists(By.TagName("h1")).Text);

        Browser.Exists(By.TagName("nav")).FindElement(By.LinkText($"Interactive component navigation ({renderMode})")).Click();
        Browser.Equal("Page with interactive components that navigate", () => Browser.Exists(By.TagName("h1")).Text);

        // Normally, you shouldn't store references to elements because they could become stale references
        // after the page re-renders. However, we want to explicitly test that the element becomes stale
        // across renders to ensure that a full page reload occurs.
        var initialRenderIdElement = Browser.Exists(By.Id("render-id"));
        var initialRenderId = -1;
        Browser.True(() => int.TryParse(initialRenderIdElement.Text, out initialRenderId));
        Assert.NotEqual(-1, initialRenderId);

        Browser.Exists(By.Id("reload-with-navigate-to")).Click();
        Browser.True(() => IsElementStale(initialRenderIdElement));

        var finalRenderIdElement = Browser.Exists(By.Id("render-id"));
        var finalRenderId = -1;
        Browser.True(() => int.TryParse(finalRenderIdElement.Text, out finalRenderId));
        Assert.NotEqual(-1, initialRenderId);
        Assert.True(finalRenderId > initialRenderId);

        // Ensure that the history stack was correctly updated
        Browser.Navigate().Back();
        Browser.Equal("Hello", () => Browser.Exists(By.TagName("h1")).Text);
        Assert.EndsWith("/nav", Browser.Url);
    }

    [Theory]
    [InlineData("server")]
    [InlineData("webassembly")]
    public void RefreshCanFallBackOnFullPageReload(string renderMode)
    {
        Navigate($"{ServerPathBase}/nav");
        Browser.Equal("Hello", () => Browser.Exists(By.TagName("h1")).Text);

        Browser.Exists(By.TagName("nav")).FindElement(By.LinkText($"Interactive component navigation ({renderMode})")).Click();
        Browser.Equal("Page with interactive components that navigate", () => Browser.Exists(By.TagName("h1")).Text);

        EnhancedNavigationTestUtil.SuppressEnhancedNavigation(this, true, skipNavigation: true);
        Browser.Navigate().Refresh();
        Browser.Equal("Page with interactive components that navigate", () => Browser.Exists(By.TagName("h1")).Text);

        // Normally, you shouldn't store references to elements because they could become stale references
        // after the page re-renders. However, we want to explicitly test that the element becomes stale
        // across renders to ensure that a full page reload occurs.
        var initialRenderIdElement = Browser.Exists(By.Id("render-id"));
        var initialRenderId = -1;
        Browser.True(() => int.TryParse(initialRenderIdElement.Text, out initialRenderId));
        Assert.NotEqual(-1, initialRenderId);

        Browser.Exists(By.Id("refresh-with-refresh")).Click();
        Browser.True(() => IsElementStale(initialRenderIdElement));

        var finalRenderIdElement = Browser.Exists(By.Id("render-id"));
        var finalRenderId = -1;
        Browser.True(() => int.TryParse(finalRenderIdElement.Text, out finalRenderId));
        Assert.NotEqual(-1, initialRenderId);
        Assert.True(finalRenderId > initialRenderId);

        // Ensure that the history stack was correctly updated
        Browser.Navigate().Back();
        Browser.Equal("Hello", () => Browser.Exists(By.TagName("h1")).Text);
        Assert.EndsWith("/nav", Browser.Url);
    }

    [Theory]
    [InlineData("server")]
    [InlineData("webassembly")]
    public void RefreshWithForceReloadDoesFullPageReload(string renderMode)
    {
        Navigate($"{ServerPathBase}/nav");
        Browser.Equal("Hello", () => Browser.Exists(By.TagName("h1")).Text);

        Browser.Exists(By.TagName("nav")).FindElement(By.LinkText($"Interactive component navigation ({renderMode})")).Click();
        Browser.Equal("Page with interactive components that navigate", () => Browser.Exists(By.TagName("h1")).Text);

        // Normally, you shouldn't store references to elements because they could become stale references
        // after the page re-renders. However, we want to explicitly test that the element becomes stale
        // across renders to ensure that a full page reload occurs.
        var initialRenderIdElement = Browser.Exists(By.Id("render-id"));
        var initialRenderId = -1;
        Browser.True(() => int.TryParse(initialRenderIdElement.Text, out initialRenderId));
        Assert.NotEqual(-1, initialRenderId);

        Browser.Exists(By.Id("reload-with-refresh")).Click();
        Browser.True(() => IsElementStale(initialRenderIdElement));

        var finalRenderIdElement = Browser.Exists(By.Id("render-id"));
        var finalRenderId = -1;
        Browser.True(() => int.TryParse(finalRenderIdElement.Text, out finalRenderId));
        Assert.NotEqual(-1, initialRenderId);
        Assert.True(finalRenderId > initialRenderId);

        // Ensure that the history stack was correctly updated
        Browser.Navigate().Back();
        Browser.Equal("Hello", () => Browser.Exists(By.TagName("h1")).Text);
        Assert.EndsWith("/nav", Browser.Url);
    }

    [Fact]
    public void CanRegisterAndRemoveEnhancedPageUpdateCallback()
    {
        Navigate($"{ServerPathBase}/nav");
        Browser.Equal("Hello", () => Browser.Exists(By.TagName("h1")).Text);

        Browser.Exists(By.TagName("nav")).FindElement(By.LinkText("Preserve content")).Click();
        Browser.Equal("Page that preserves content", () => Browser.Exists(By.TagName("h1")).Text);

        // Required until https://github.com/dotnet/aspnetcore/issues/50424 is fixed
        Browser.Navigate().Refresh();

        Browser.Exists(By.Id("refresh-with-refresh"));

        Browser.Click(By.Id("start-listening"));

        Browser.Click(By.Id("refresh-with-refresh"));
        AssertEnhancedUpdateCountEquals(1);

        Browser.Click(By.Id("refresh-with-refresh"));
        AssertEnhancedUpdateCountEquals(2);

        Browser.Click(By.Id("stop-listening"));

        Browser.Click(By.Id("refresh-with-refresh"));
        AssertEnhancedUpdateCountEquals(2);

        Browser.Click(By.Id("refresh-with-refresh"));
        AssertEnhancedUpdateCountEquals(2);

        void AssertEnhancedUpdateCountEquals(long count)
            => Browser.Equal(count, () => ((IJavaScriptExecutor)Browser).ExecuteScript("return window.enhancedPageUpdateCount;"));
    }

    [Fact]
    public void ElementsWithDataPermanentAttribute_HavePreservedContent()
    {
        Navigate($"{ServerPathBase}/nav");
        Browser.Equal("Hello", () => Browser.Exists(By.TagName("h1")).Text);

        Browser.Exists(By.TagName("nav")).FindElement(By.LinkText("Preserve content")).Click();
        Browser.Equal("Page that preserves content", () => Browser.Exists(By.TagName("h1")).Text);

        // Required until https://github.com/dotnet/aspnetcore/issues/50424 is fixed
        Browser.Navigate().Refresh();

        Browser.Exists(By.Id("refresh-with-refresh"));

        Browser.Click(By.Id("start-listening"));

        Browser.Click(By.Id("refresh-with-refresh"));
        AssertEnhancedUpdateCountEquals(1);

        Browser.Equal("Preserved content", () => Browser.Exists(By.Id("preserved-content")).Text);

        Browser.Click(By.Id("refresh-with-refresh"));
        AssertEnhancedUpdateCountEquals(2);

        Browser.Equal("Preserved content", () => Browser.Exists(By.Id("preserved-content")).Text);
    }

    [Fact]
    public void ElementsWithoutDataPermanentAttribute_DoNotHavePreservedContent()
    {
        Navigate($"{ServerPathBase}/nav");
        Browser.Equal("Hello", () => Browser.Exists(By.TagName("h1")).Text);

        Browser.Exists(By.TagName("nav")).FindElement(By.LinkText("Preserve content")).Click();
        Browser.Equal("Page that preserves content", () => Browser.Exists(By.TagName("h1")).Text);

        // Required until https://github.com/dotnet/aspnetcore/issues/50424 is fixed
        Browser.Navigate().Refresh();

        Browser.Exists(By.Id("refresh-with-refresh"));

        Browser.Click(By.Id("start-listening"));

        Browser.Equal("Non preserved content", () => Browser.Exists(By.Id("non-preserved-content")).Text);

        Browser.Click(By.Id("refresh-with-refresh"));
        AssertEnhancedUpdateCountEquals(1);

        Browser.Equal("", () => Browser.Exists(By.Id("non-preserved-content")).Text);
    }

    [Fact]
    public void EnhancedNavNotUsedForNonBlazorDestinations()
    {
        Navigate($"{ServerPathBase}/nav");
        Browser.Equal("Hello", () => Browser.Exists(By.TagName("h1")).Text);
        Assert.Equal("object", Browser.ExecuteJavaScript<string>("return typeof Blazor")); // Blazor JS is loaded

        Browser.Exists(By.TagName("nav")).FindElement(By.LinkText("Non-Blazor HTML page")).Click();
        Browser.Equal("This is a non-Blazor endpoint", () => Browser.Exists(By.TagName("h1")).Text);
        Assert.Equal("undefined", Browser.ExecuteJavaScript<string>("return typeof Blazor")); // Blazor JS is NOT loaded
    }

    [Theory]
    [InlineData("server")]
    [InlineData("wasm")]
    public void LocationChangedEventGetsInvokedOnEnhancedNavigation_OnlyServerOrWebAssembly(string renderMode)
    {
        Navigate($"{ServerPathBase}/nav");
        Browser.Equal("Hello", () => Browser.Exists(By.TagName("h1")).Text);

        Browser.Exists(By.TagName("nav")).FindElement(By.LinkText($"LocationChanged/LocationChanging event ({renderMode})")).Click();
        Browser.Equal("Page with location changed components", () => Browser.Exists(By.TagName("h1")).Text);
        Browser.Equal("0", () => Browser.Exists(By.Id($"location-changed-count-{renderMode}")).Text);

        Browser.Exists(By.Id($"update-query-string-{renderMode}")).Click();

        Browser.Equal("1", () => Browser.Exists(By.Id($"location-changed-count-{renderMode}")).Text);
    }

    [Theory]
    [InlineData("server")]
    [InlineData("wasm")]
    public void LocationChangedEventGetsInvokedOnEnhancedNavigation_BothServerAndWebAssembly(string runtimeThatInvokedNavigation)
    {
        Navigate($"{ServerPathBase}/nav");
        Browser.Equal("Hello", () => Browser.Exists(By.TagName("h1")).Text);

        Browser.Exists(By.TagName("nav")).FindElement(By.LinkText("LocationChanged/LocationChanging event (server-and-wasm)")).Click();
        Browser.Equal("Page with location changed components", () => Browser.Exists(By.TagName("h1")).Text);
        Browser.Equal("0", () => Browser.Exists(By.Id("location-changed-count-server")).Text);
        Browser.Equal("0", () => Browser.Exists(By.Id("location-changed-count-wasm")).Text);

        Browser.Exists(By.Id($"update-query-string-{runtimeThatInvokedNavigation}")).Click();

        // LocationChanged event gets invoked for both interactive runtimes
        Browser.Equal("1", () => Browser.Exists(By.Id("location-changed-count-server")).Text);
        Browser.Equal("1", () => Browser.Exists(By.Id("location-changed-count-wasm")).Text);
    }

    [Theory]
    [InlineData("server")]
    [InlineData("wasm")]
    public void NavigationManagerUriGetsUpdatedOnEnhancedNavigation_OnlyServerOrWebAssembly(string renderMode)
    {
        Navigate($"{ServerPathBase}/nav");
        Browser.Equal("Hello", () => Browser.Exists(By.TagName("h1")).Text);

        Browser.Exists(By.TagName("nav")).FindElement(By.LinkText($"LocationChanged/LocationChanging event ({renderMode})")).Click();
        Browser.Equal("Page with location changed components", () => Browser.Exists(By.TagName("h1")).Text);
        Assert.EndsWith($"/nav/location-changed/{renderMode}", Browser.Exists(By.Id($"nav-uri-{renderMode}")).Text);

        Browser.Exists(By.Id($"update-query-string-{renderMode}")).Click();

        Assert.EndsWith($"/nav/location-changed/{renderMode}?query=1", Browser.Exists(By.Id($"nav-uri-{renderMode}")).Text);
    }

    [Theory]
    [InlineData("server")]
    [InlineData("wasm")]
    public void NavigationManagerUriGetsUpdatedOnEnhancedNavigation_BothServerAndWebAssembly(string runtimeThatInvokedNavigation)
    {
        Navigate($"{ServerPathBase}/nav");
        Browser.Equal("Hello", () => Browser.Exists(By.TagName("h1")).Text);

        Browser.Exists(By.TagName("nav")).FindElement(By.LinkText("LocationChanged/LocationChanging event (server-and-wasm)")).Click();
        Browser.Equal("Page with location changed components", () => Browser.Exists(By.TagName("h1")).Text);
        Assert.EndsWith("/nav/location-changed/server-and-wasm", Browser.Exists(By.Id("nav-uri-server")).Text);
        Assert.EndsWith("/nav/location-changed/server-and-wasm", Browser.Exists(By.Id("nav-uri-wasm")).Text);

        Browser.Exists(By.Id($"update-query-string-{runtimeThatInvokedNavigation}")).Click();

        Assert.EndsWith($"/nav/location-changed/server-and-wasm?query=1", Browser.Exists(By.Id($"nav-uri-server")).Text);
        Assert.EndsWith($"/nav/location-changed/server-and-wasm?query=1", Browser.Exists(By.Id($"nav-uri-wasm")).Text);
    }

    [Theory]
    [InlineData("server")]
    [InlineData("wasm")]
    public void SupplyParameterFromQueryGetsUpdatedOnEnhancedNavigation_OnlyServerOrWebAssembly(string renderMode)
    {
        Navigate($"{ServerPathBase}/nav");
        Browser.Equal("Hello", () => Browser.Exists(By.TagName("h1")).Text);

        Browser.Exists(By.TagName("nav")).FindElement(By.LinkText($"LocationChanged/LocationChanging event ({renderMode})")).Click();
        Browser.Equal("Page with location changed components", () => Browser.Exists(By.TagName("h1")).Text);

        Browser.Exists(By.Id($"update-query-string-{renderMode}")).Click();
        Browser.Equal("1", () => Browser.Exists(By.Id($"query-{renderMode}")).Text);

        Browser.Exists(By.Id($"update-query-string-{renderMode}")).Click();
        Browser.Equal("2", () => Browser.Exists(By.Id($"query-{renderMode}")).Text);
    }

    [Theory]
    [InlineData("server")]
    [InlineData("wasm")]
    public void SupplyParameterFromQueryGetsUpdatedOnEnhancedNavigation_BothServerAndWebAssembly(string runtimeThatInvokedNavigation)
    {
        Navigate($"{ServerPathBase}/nav");
        Browser.Equal("Hello", () => Browser.Exists(By.TagName("h1")).Text);

        Browser.Exists(By.TagName("nav")).FindElement(By.LinkText("LocationChanged/LocationChanging event (server-and-wasm)")).Click();
        Browser.Equal("Page with location changed components", () => Browser.Exists(By.TagName("h1")).Text);

        Browser.Exists(By.Id($"update-query-string-{runtimeThatInvokedNavigation}")).Click();
        Browser.Equal("1", () => Browser.Exists(By.Id("query-server")).Text);
        Browser.Equal("1", () => Browser.Exists(By.Id("query-wasm")).Text);

        Browser.Exists(By.Id($"update-query-string-{runtimeThatInvokedNavigation}")).Click();
        Browser.Equal("2", () => Browser.Exists(By.Id("query-server")).Text);
        Browser.Equal("2", () => Browser.Exists(By.Id("query-wasm")).Text);
    }

    [Theory]
    [InlineData("server")]
    [InlineData("wasm")]
    public void LocationChangingEventGetsInvokedOnEnhancedNavigation_OnlyServerOrWebAssembly(string renderMode)
    {
        Navigate($"{ServerPathBase}/nav");
        Browser.Equal("Hello", () => Browser.Exists(By.TagName("h1")).Text);

        Browser.Exists(By.TagName("nav")).FindElement(By.LinkText($"LocationChanged/LocationChanging event ({renderMode})")).Click();
        Browser.Equal("Page with location changed components", () => Browser.Exists(By.TagName("h1")).Text);
        Browser.Equal("0", () => Browser.Exists(By.Id($"location-changing-count-{renderMode}")).Text);

        Browser.Exists(By.Id($"update-query-string-{renderMode}")).Click();

        Browser.Equal("1", () => Browser.Exists(By.Id($"location-changing-count-{renderMode}")).Text);
    }

    [Theory]
    [InlineData("server")]
    [InlineData("wasm")]
    public void LocationChangingEventGetsInvokedOnEnhancedNavigationOnlyForRuntimeThatInvokedNavigation(string runtimeThatInvokedNavigation)
    {
        Navigate($"{ServerPathBase}/nav");
        Browser.Equal("Hello", () => Browser.Exists(By.TagName("h1")).Text);

        Browser.Exists(By.TagName("nav")).FindElement(By.LinkText("LocationChanged/LocationChanging event (server-and-wasm)")).Click();
        Browser.Equal("Page with location changed components", () => Browser.Exists(By.TagName("h1")).Text);
        Browser.Equal("0", () => Browser.Exists(By.Id("location-changing-count-server")).Text);
        Browser.Equal("0", () => Browser.Exists(By.Id("location-changing-count-wasm")).Text);

        Browser.Exists(By.Id($"update-query-string-{runtimeThatInvokedNavigation}")).Click();

        // LocationChanging event gets invoked only for the interactive runtime that invoked navigation
        var anotherRuntime = runtimeThatInvokedNavigation == "server" ? "wasm" : "server";
        Browser.Equal("1", () => Browser.Exists(By.Id($"location-changing-count-{runtimeThatInvokedNavigation}")).Text);
        Browser.Equal("0", () => Browser.Exists(By.Id($"location-changing-count-{anotherRuntime}")).Text);
    }

    [Theory]
    [InlineData("server")]
    [InlineData("wasm")]
    public void CanReceiveNullParameterValueOnEnhancedNavigation(string renderMode)
    {
        // See: https://github.com/dotnet/aspnetcore/issues/52434
        Navigate($"{ServerPathBase}/nav");
        Browser.Equal("Hello", () => Browser.Exists(By.TagName("h1")).Text);

        Browser.Exists(By.TagName("nav")).FindElement(By.LinkText($"Null component parameter ({renderMode})")).Click();
        Browser.Equal("Page rendering component with null parameter", () => Browser.Exists(By.TagName("h1")).Text);
        Browser.Equal("0", () => Browser.Exists(By.Id("current-count")).Text);

        Browser.Exists(By.Id("button-increment")).Click();
        Browser.Equal("0", () => Browser.Exists(By.Id("location-changed-count")).Text);
        Browser.Equal("1", () => Browser.Exists(By.Id("current-count")).Text);

        // This refresh causes the interactive component to receive a 'null' parameter value
        Browser.Exists(By.Id("button-refresh")).Click();
        Browser.Equal("1", () => Browser.Exists(By.Id("location-changed-count")).Text);
        Browser.Equal("1", () => Browser.Exists(By.Id("current-count")).Text);

        // Increment the count again to ensure that interactivity still works
        Browser.Exists(By.Id("button-increment")).Click();
        Browser.Equal("2", () => Browser.Exists(By.Id("current-count")).Text);

        // Even if the interactive runtime continues to function (as the WebAssembly runtime might),
        // fail the test if any errors were logged to the browser console
        var logs = Browser.GetBrowserLogs(LogLevel.Warning);
        Assert.DoesNotContain(logs, log => log.Message.Contains("Error"));
    }

    [Fact]
    public void CanUpdateHrefOnLinkTagWithIntegrity()
    {
        // Represents issue https://github.com/dotnet/aspnetcore/issues/54250
        // Previously, if the "integrity" attribute appeared after "href", then we'd be unable
        // to update "href" because the new content wouldn't match the existing "integrity".
        // This is fixed by ensuring we update "integrity" first in all cases.

        Navigate($"{ServerPathBase}/nav/page-with-link-tag/1");

        var originalH1Elem = Browser.Exists(By.TagName("h1"));
        Browser.Equal("PageWithLinkTag 1", () => originalH1Elem.Text);
        Browser.Equal("rgba(255, 0, 0, 1)", () => originalH1Elem.GetCssValue("color"));

        Browser.Exists(By.LinkText("Go to page with link tag 2")).Click();
        Browser.Equal("PageWithLinkTag 2", () => originalH1Elem.Text);
        Browser.Equal("rgba(0, 0, 255, 1)", () => originalH1Elem.GetCssValue("color"));
    }

    private void AssertEnhancedUpdateCountEquals(long count)
        => Browser.Equal(count, () => ((IJavaScriptExecutor)Browser).ExecuteScript("return window.enhancedPageUpdateCount;"));

    private static bool IsElementStale(IWebElement element)
    {
        try
        {
            _ = element.Enabled;
            return false;
        }
        catch (StaleElementReferenceException)
        {
            return true;
        }
    }
}
