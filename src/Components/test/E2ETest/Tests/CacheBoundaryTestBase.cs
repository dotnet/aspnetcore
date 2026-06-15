// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using Components.TestServer.RazorComponents;
using Microsoft.AspNetCore.Components.E2ETest.Infrastructure;
using Microsoft.AspNetCore.Components.E2ETest.Infrastructure.ServerFixtures;
using Microsoft.AspNetCore.E2ETesting;
using OpenQA.Selenium;
using TestServer;
using Xunit.Abstractions;

namespace Microsoft.AspNetCore.Components.E2ETest.Tests;

public abstract class CacheBoundaryTestBase : ServerTestBase<BasicTestAppServerSiteFixture<RazorComponentEndpointsNoInteractivityStartup<App>>>
{
    protected CacheBoundaryTestBase(
        BrowserFixture browserFixture,
        BasicTestAppServerSiteFixture<RazorComponentEndpointsNoInteractivityStartup<App>> serverFixture,
        ITestOutputHelper output)
        : base(browserFixture, serverFixture, output)
    {
    }

    public override Task InitializeAsync() => InitializeAsync(BrowserFixture.StreamingContext);

    protected override void InitializeAsyncCore()
    {
        ConfigureServerArguments();
        base.InitializeAsyncCore();
        Navigate($"{ServerPathBase}/cache-component/clear");
    }

    // Hook for derived classes to select the cache store backing the server (e.g. HybridCache).
    protected virtual void ConfigureServerArguments()
    {
    }

    [Fact]
    public void CacheBoundaryCachesData()
    {
        Navigate($"{ServerPathBase}/cache-component");
        var testElement = Browser.FindElement(By.Id("test-1"));
        var cachedValue = testElement.FindElement(By.CssSelector(".cached")).Text;

        Navigate($"{ServerPathBase}/cache-component");
        Browser.Equal(cachedValue, () => Browser.FindElement(By.Id("test-1")).FindElement(By.CssSelector(".cached")).Text);
        Browser.NotEqual(cachedValue, () => Browser.FindElement(By.Id("test-1")).FindElement(By.CssSelector(".not-cached")).Text);
        Browser.NotEqual(cachedValue, () => Browser.FindElement(By.Id("test-1")).FindElement(By.CssSelector(".not-cache-component")).Text);
    }

    [Fact]
    public void CacheBoundaryDoesNotCacheDataWhenNotEnabled()
    {
        Navigate($"{ServerPathBase}/cache-component");
        var testElement = Browser.FindElement(By.Id("test-2"));
        var firstValue = testElement.FindElement(By.CssSelector(".cached")).Text;

        Navigate($"{ServerPathBase}/cache-component");
        Browser.NotEqual(firstValue, () => Browser.FindElement(By.Id("test-2")).FindElement(By.CssSelector(".cached")).Text);
        Browser.NotEqual(firstValue, () => Browser.FindElement(By.Id("test-2")).FindElement(By.CssSelector(".not-cached")).Text);
        Browser.NotEqual(firstValue, () => Browser.FindElement(By.Id("test-2")).FindElement(By.CssSelector(".not-cache-component")).Text);
    }

    [Fact]
    public void CacheBoundaryCorrectlyCreatesHoles()
    {
        Navigate($"{ServerPathBase}/cache-component");
        var testElement = Browser.FindElement(By.Id("test-3"));
        Browser.Equal("never", () => testElement.FindElement(By.Id("message")).Text);
        testElement.FindElement(By.Id("message-input")).SendKeys("new message");
        testElement.FindElement(By.Id("submit")).Click();

        Browser.Equal("new message", () => Browser.FindElement(By.Id("test-3")).FindElement(By.Id("message")).Text);
        testElement = Browser.FindElement(By.Id("test-3"));
        testElement.FindElement(By.Id("message-input")).SendKeys("cache hit");
        testElement.FindElement(By.Id("submit")).Click();

        Browser.Equal("cache hit", () => Browser.FindElement(By.Id("test-3")).FindElement(By.Id("message")).Text);
    }

    [Fact]
    public void NestedCacheBoundaryDoesNotExecuteOnOuterCacheHit()
    {
        Navigate($"{ServerPathBase}/cache-component");
        Browser.Exists(By.Id("inner-cached"));
        var renderCount = GetRenderCount();
        Assert.Equal(1, renderCount);

        Navigate($"{ServerPathBase}/cache-component");
        Browser.Exists(By.Id("inner-cached"));
        renderCount = GetRenderCount();
        Assert.Equal(1, renderCount);
    }

    [Fact]
    public void CacheBoundaryInLoopUsesVaryByForDistinctEntries()
    {
        Navigate($"{ServerPathBase}/cache-component");
        var loopItems = Browser.FindElement(By.Id("test-5")).FindElements(By.CssSelector(".loop-item"));
        Assert.Equal(3, loopItems.Count);

        // Each iteration should have its own distinct cached value
        var firstRenderValues = new string[3];
        for (var i = 0; i < 3; i++)
        {
            firstRenderValues[i] = loopItems[i].FindElement(By.CssSelector(".cached-value")).Text;
        }
        Assert.Equal(3, firstRenderValues.Distinct().Count());

        // Second navigation — each entry should be independently cached
        Navigate($"{ServerPathBase}/cache-component");
        for (var i = 0; i < 3; i++)
        {
            var index = i;
            Browser.Equal(firstRenderValues[index], () =>
                Browser.FindElement(By.Id("test-5"))
                    .FindElements(By.CssSelector(".loop-item"))[index]
                    .FindElement(By.CssSelector(".cached-value")).Text);
        }
    }

    [Fact]
    public void CacheBoundaryMultipleHolesOfSameType_PreserveCorrectOrder()
    {
        Navigate($"{ServerPathBase}/cache-component");
        Browser.Equal("first", () => Browser.FindElement(By.Id("test-8")).FindElement(By.CssSelector(".hole-0")).Text);
        Browser.Equal("second", () => Browser.FindElement(By.Id("test-8")).FindElement(By.CssSelector(".hole-1")).Text);
        var cachedContent = Browser.FindElement(By.Id("test-8")).FindElement(By.CssSelector(".cached-content")).Text;

        // Cache hit — holes with same (TypeName, Sequence) must not be swapped
        Navigate($"{ServerPathBase}/cache-component");
        Browser.Equal(cachedContent, () => Browser.FindElement(By.Id("test-8")).FindElement(By.CssSelector(".cached-content")).Text);
        Browser.Equal("first", () => Browser.FindElement(By.Id("test-8")).FindElement(By.CssSelector(".hole-0")).Text);
        Browser.Equal("second", () => Browser.FindElement(By.Id("test-8")).FindElement(By.CssSelector(".hole-1")).Text);
    }

    [Fact]
    public void ReusableComponentWithCacheBoundary_UsedTwice_SharesOneCacheEntry()
    {
        // A reusable component that internally contains a CacheBoundary (no CacheKey), used twice on
        // a page, produces two boundaries that resolve to the SAME cache key with no user error.
        // They must share a single cache entry (and must not hang the request). On the first (cold)
        // render the creator produces the entry and the sibling renders fresh; on every subsequent
        // (warm) request both boundaries are served from the one shared entry.
        Navigate($"{ServerPathBase}/cache-component");
        Browser.Exists(By.Id("test-9"));
        Browser.Equal(2, () => Browser.FindElement(By.Id("test-9")).FindElements(By.CssSelector(".panel-content")).Count);

        // Cold render: each instance rendered its own content.
        Assert.Equal(new[] { "alpha", "beta" }, GetPanelTexts(".panel-content"));
        var creatorGuid = GetPanelTexts(".panel-guid")[0];

        // Warm reload: both boundaries share the one cached entry, so both show the creator's cached
        // content and the identical cached guid.
        Navigate($"{ServerPathBase}/cache-component");
        Browser.Exists(By.Id("test-9"));
        Browser.Equal(2, () => Browser.FindElement(By.Id("test-9")).FindElements(By.CssSelector(".panel-content")).Count);

        Browser.Equal(new[] { "alpha", "alpha" }, () => GetPanelTexts(".panel-content").ToArray());
        Assert.Equal(new[] { creatorGuid, creatorGuid }, GetPanelTexts(".panel-guid"));
    }

    private int GetRenderCount()
    {
        Navigate($"{ServerPathBase}/cache-component/render-count");
        var body = Browser.FindElement(By.TagName("body")).Text;
        return int.Parse(body, CultureInfo.InvariantCulture);
    }

    private List<string> GetPanelTexts(string selector)
        => Browser.FindElement(By.Id("test-9"))
            .FindElements(By.CssSelector(selector))
            .Select(panel => panel.Text)
            .ToList();
}
