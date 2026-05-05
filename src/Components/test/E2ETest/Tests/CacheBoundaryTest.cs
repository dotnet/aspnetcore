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

public class CacheBoundaryTest : ServerTestBase<BasicTestAppServerSiteFixture<RazorComponentEndpointsStartup<App>>>
{
    public CacheBoundaryTest(
        BrowserFixture browserFixture,
        BasicTestAppServerSiteFixture<RazorComponentEndpointsStartup<App>> serverFixture,
        ITestOutputHelper output)
        : base(browserFixture, serverFixture, output)
    {
    }

    public override Task InitializeAsync() => InitializeAsync(BrowserFixture.StreamingContext);

    protected override void InitializeAsyncCore()
    {
        base.InitializeAsyncCore();
        Navigate($"{ServerPathBase}/cache-component/clear");
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
    public void CacheBoundary_HoleShapeShrinks_SkipsUnmatchedHoleAndKeepsOtherWidgets()
    {
        Navigate($"{ServerPathBase}/cache-component");
        var test6 = Browser.FindElement(By.Id("test-6"));
        Assert.Equal(3, test6.FindElements(By.CssSelector(".loop-label")).Count);
        Assert.Equal(3, test6.FindElements(By.CssSelector(".loop-widget")).Count);

        // Server-side: drop the last item so ChildContent now produces 2 holes instead of 3.
        Navigate($"{ServerPathBase}/cache-component/drop-item");

        Navigate($"{ServerPathBase}/cache-component");
        test6 = Browser.FindElement(By.Id("test-6"));
        var labels = test6.FindElements(By.CssSelector(".loop-label"));
        var widgets = test6.FindElements(By.CssSelector(".loop-widget"));

        Assert.Equal(3, labels.Count);
        Browser.Equal("widget-a", () => Browser.FindElement(By.Id("test-6")).FindElements(By.CssSelector(".loop-widget"))[0].Text);
        Browser.Equal("widget-b", () => Browser.FindElement(By.Id("test-6")).FindElements(By.CssSelector(".loop-widget"))[1].Text);
        Assert.Equal(2, widgets.Count);
    }

    [Fact]
    public void CacheBoundary_PreservesInteractivity_OfRenderModeChild_OnCacheHit()
    {
        Navigate($"{ServerPathBase}/cache-component");
        Browser.Equal("True", () => Browser.FindElement(By.Id("is-interactive-server")).Text);
        Browser.Equal("0", () => Browser.FindElement(By.Id("count-server")).Text);

        var firstCachedMarker = Browser.FindElement(By.Id("test-7-cached")).Text;
        var firstNonCachedMarker = Browser.FindElement(By.Id("test-7-non-cached")).Text;

        Browser.Click(By.Id("increment-server"));
        Browser.Equal("1", () => Browser.FindElement(By.Id("count-server")).Text);

        Navigate($"{ServerPathBase}/cache-component");

        Browser.Equal(firstCachedMarker, () => Browser.FindElement(By.Id("test-7-cached")).Text);
        Browser.NotEqual(firstNonCachedMarker, () => Browser.FindElement(By.Id("test-7-non-cached")).Text);

        Browser.Equal("True", () => Browser.FindElement(By.Id("is-interactive-server")).Text);
        Browser.Equal("0", () => Browser.FindElement(By.Id("count-server")).Text);

        Browser.Click(By.Id("increment-server"));
        Browser.Equal("1", () => Browser.FindElement(By.Id("count-server")).Text);
    }

    private int GetRenderCount()
    {
        Navigate($"{ServerPathBase}/cache-component/render-count");
        var body = Browser.FindElement(By.TagName("body")).Text;
        return int.Parse(body, CultureInfo.InvariantCulture);
    }
}
