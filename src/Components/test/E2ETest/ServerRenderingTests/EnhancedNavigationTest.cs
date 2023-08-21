// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components.E2ETest.Infrastructure.ServerFixtures;
using Microsoft.AspNetCore.Components.E2ETest.Infrastructure;
using Microsoft.AspNetCore.E2ETesting;
using TestServer;
using Xunit.Abstractions;
using Components.TestServer.RazorComponents;
using OpenQA.Selenium;
using System.Globalization;

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
    public void ScrollsToHashWithContentAddedAsynchronously()
    {
        Navigate($"{ServerPathBase}/nav");
        Browser.Exists(By.TagName("nav")).FindElement(By.LinkText("Scroll to hash")).Click();
        Assert.Equal(0, BrowserScrollY);

        var asyncContentHeader = Browser.Exists(By.Id("some-content"));
        Browser.Equal("Some content", () => asyncContentHeader.Text);
        Browser.True(() => BrowserScrollY > 500);
    }

    [Fact]
    public void CanFollowSynchronousRedirection()
    {
        Navigate($"{ServerPathBase}/nav");

        var h1Elem = Browser.Exists(By.TagName("h1"));
        Browser.Equal("Hello", () => h1Elem.Text);

        // Click a link and show we redirected, preserving elements, and updating the URL
        // Note that in this specific case we can't preserve the hash part of the URL, as it
        // gets lost when the browser follows a 'fetch' redirection. If we decide it's important
        // to support this later, we'd have to change the server not to do a real redirection
        // here and instead use the same protocol it uses for external redirections.
        Browser.Exists(By.TagName("nav")).FindElement(By.LinkText("Redirect")).Click();
        Browser.Equal("Scroll to hash", () => h1Elem.Text);
        Assert.EndsWith("/subdir/nav/scroll-to-hash", Browser.Url);

        // See that 'back' takes you to the place from before the redirection
        Browser.Navigate().Back();
        Browser.Equal("Hello", () => h1Elem.Text);
        Assert.EndsWith("/subdir/nav", Browser.Url);
    }

    [Fact]
    public void CanFollowAsynchronousRedirectionWhileStreaming()
    {
        Navigate($"{ServerPathBase}/nav");

        var h1Elem = Browser.Exists(By.TagName("h1"));
        Browser.Equal("Hello", () => h1Elem.Text);

        // Click a link and show we redirected, preserving elements, scrolling to hash, and updating the URL
        Browser.Exists(By.TagName("nav")).FindElement(By.LinkText("Redirect while streaming")).Click();
        Browser.Equal("Scroll to hash", () => h1Elem.Text);
        Browser.True(() => BrowserScrollY > 500);
        Assert.EndsWith("/subdir/nav/scroll-to-hash#some-content", Browser.Url);

        // See that 'back' takes you to the place from before the redirection
        Browser.Navigate().Back();
        Browser.Equal("Hello", () => h1Elem.Text);
        Assert.EndsWith("/subdir/nav", Browser.Url);
    }

    [Fact]
    public void CanFollowSynchronousExternalRedirection()
    {
        Navigate($"{ServerPathBase}/nav");
        Browser.Equal("Hello", () => Browser.Exists(By.TagName("h1")).Text);

        Browser.Exists(By.TagName("nav")).FindElement(By.LinkText("Redirect external")).Click();
        Browser.Contains("microsoft.com", () => Browser.Url);
    }

    [Fact]
    public void CanFollowAsynchronousExternalRedirectionWhileStreaming()
    {
        Navigate($"{ServerPathBase}/nav");
        Browser.Equal("Hello", () => Browser.Exists(By.TagName("h1")).Text);

        Browser.Exists(By.TagName("nav")).FindElement(By.LinkText("Redirect external while streaming")).Click();
        Browser.Contains("microsoft.com", () => Browser.Url);
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
        
        ((IJavaScriptExecutor)Browser).ExecuteScript("sessionStorage.setItem('suppress-enhanced-navigation', 'true')");
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

    private long BrowserScrollY
    {
        get => Convert.ToInt64(((IJavaScriptExecutor)Browser).ExecuteScript("return window.scrollY"), CultureInfo.CurrentCulture);
        set => ((IJavaScriptExecutor)Browser).ExecuteScript($"window.scrollTo(0, {value})");
    }

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
