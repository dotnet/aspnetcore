// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Components.TestServer.RazorComponents;
using Microsoft.AspNetCore.Components.E2ETest.Infrastructure;
using Microsoft.AspNetCore.Components.E2ETest.Infrastructure.ServerFixtures;
using Microsoft.AspNetCore.E2ETesting;
using OpenQA.Selenium;
using TestServer;
using Xunit.Abstractions;

namespace Microsoft.AspNetCore.Components.E2ETest.Tests;

public abstract class CacheViewTestBase : ServerTestBase<BasicTestAppServerSiteFixture<RazorComponentEndpointsNoInteractivityStartup<App>>>
{
    // A unique id per test instance. Every CacheView on the test pages varies by the "testId" query
    // parameter, so each test's cache entries are isolated. This lets the suite run without relying on a
    // shared, globally-cleared cache and keeps tests independent when executed concurrently.
    private readonly string _testId = Guid.NewGuid().ToString("N");

    protected CacheViewTestBase(
        BrowserFixture browserFixture,
        BasicTestAppServerSiteFixture<RazorComponentEndpointsNoInteractivityStartup<App>> serverFixture,
        ITestOutputHelper output)
        : base(browserFixture, serverFixture, output)
    {
    }

    // The streaming context uses PageLoadStrategy.None so navigation does not block on the load event while
    // content is still streaming in. This is required by the tests that assert on streaming-rendered content.
    public override Task InitializeAsync() => InitializeAsync(BrowserFixture.StreamingContext);

    protected override void InitializeAsyncCore()
    {
        ConfigureServerArguments();
        base.InitializeAsyncCore();
    }

    // Hook for derived classes to select the cache store backing the server (e.g. HybridCache).
    protected virtual void ConfigureServerArguments()
    {
    }

    // Builds a URL for the given page path, appending this test's unique testId so cache entries and the
    // render counter are isolated per test.
    private string TestUrl(string path)
    {
        var separator = path.Contains('?', StringComparison.Ordinal) ? '&' : '?';
        return $"{ServerPathBase}/{path}{separator}testId={_testId}";
    }

    [Fact]
    public void CacheViewCachesData()
    {
        Navigate(TestUrl("cache-component"));
        var testElement = Browser.FindElement(By.Id("test-1"));
        var cachedValue = testElement.FindElement(By.CssSelector(".cached")).Text;

        Navigate(TestUrl("cache-component"));
        Browser.Equal(cachedValue, () => Browser.FindElement(By.Id("test-1")).FindElement(By.CssSelector(".cached")).Text);
        Browser.NotEqual(cachedValue, () => Browser.FindElement(By.Id("test-1")).FindElement(By.CssSelector(".not-cached")).Text);
        Browser.NotEqual(cachedValue, () => Browser.FindElement(By.Id("test-1")).FindElement(By.CssSelector(".not-cache-component")).Text);
    }

    [Fact]
    public void CacheViewDoesNotCacheDataWhenNotEnabled()
    {
        Navigate(TestUrl("cache-component"));
        var testElement = Browser.FindElement(By.Id("test-2"));
        var firstValue = testElement.FindElement(By.CssSelector(".cached")).Text;

        Navigate(TestUrl("cache-component"));
        Browser.NotEqual(firstValue, () => Browser.FindElement(By.Id("test-2")).FindElement(By.CssSelector(".cached")).Text);
        Browser.NotEqual(firstValue, () => Browser.FindElement(By.Id("test-2")).FindElement(By.CssSelector(".not-cached")).Text);
        Browser.NotEqual(firstValue, () => Browser.FindElement(By.Id("test-2")).FindElement(By.CssSelector(".not-cache-component")).Text);
    }

    [Fact]
    public void CacheViewCorrectlyCreatesLiveCachedComponents()
    {
        Navigate(TestUrl("cache-component"));
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
    public void EditFormWithFormComponents_CachesStaticContent_AndFormStillSubmits()
    {
        Navigate(TestUrl("cache-component-form"));
        var cachedGuid = Browser.FindElement(By.Id("test-form-in-cache")).FindElement(By.CssSelector(".form-cached-guid")).Text;
        // The DisplayName form component rendered inside the cache.
        Browser.Equal("Message", () => Browser.FindElement(By.Id("test-form-in-cache")).FindElement(By.CssSelector(".form-display-name")).Text);
        Browser.Equal("never", () => Browser.FindElement(By.Id("test-form-in-cache")).FindElement(By.Id("cached-form-message")).Text);

        // Warm reload: the cached form content (static guid + form components) is served from the cache.
        Navigate(TestUrl("cache-component-form"));
        Browser.Equal(cachedGuid, () => Browser.FindElement(By.Id("test-form-in-cache")).FindElement(By.CssSelector(".form-cached-guid")).Text);
        Browser.Equal("Message", () => Browser.FindElement(By.Id("test-form-in-cache")).FindElement(By.CssSelector(".form-display-name")).Text);

        // The form still submits: the POST renders live and dispatches to OnValidSubmit.
        var form = Browser.FindElement(By.Id("test-form-in-cache"));
        form.FindElement(By.Id("cached-form-input")).SendKeys("hello");
        form.FindElement(By.Id("cached-form-submit")).Click();
        Browser.Equal("hello", () => Browser.FindElement(By.Id("test-form-in-cache")).FindElement(By.Id("cached-form-message")).Text);
    }

    [Fact]
    public void CacheViewInLoopUsesVaryByForDistinctEntries()
    {
        Navigate(TestUrl("cache-component"));
        var loopItems = Browser.FindElement(By.Id("test-4")).FindElements(By.CssSelector(".loop-item"));
        Assert.Equal(3, loopItems.Count);

        // Each iteration should have its own distinct cached value
        var firstRenderValues = new string[3];
        for (var i = 0; i < 3; i++)
        {
            firstRenderValues[i] = loopItems[i].FindElement(By.CssSelector(".cached-value")).Text;
        }
        Assert.Equal(3, firstRenderValues.Distinct().Count());

        // Second navigation — each entry should be independently cached
        Navigate(TestUrl("cache-component"));
        for (var i = 0; i < 3; i++)
        {
            var index = i;
            Browser.Equal(firstRenderValues[index], () =>
                Browser.FindElement(By.Id("test-4"))
                    .FindElements(By.CssSelector(".loop-item"))[index]
                    .FindElement(By.CssSelector(".cached-value")).Text);
        }
    }

    [Fact]
    public void CacheViewMultipleLiveCachedComponentsOfSameType_PreserveCorrectOrder()
    {
        Navigate(TestUrl("cache-component"));
        Browser.Equal("first", () => Browser.FindElement(By.Id("test-5")).FindElement(By.CssSelector(".live-cached-0")).Text);
        Browser.Equal("second", () => Browser.FindElement(By.Id("test-5")).FindElement(By.CssSelector(".live-cached-1")).Text);
        var cachedContent = Browser.FindElement(By.Id("test-5")).FindElement(By.CssSelector(".cached-content")).Text;

        // Cache hit — live cached components with same (TypeName, Sequence) must not be swapped
        Navigate(TestUrl("cache-component"));
        Browser.Equal(cachedContent, () => Browser.FindElement(By.Id("test-5")).FindElement(By.CssSelector(".cached-content")).Text);
        Browser.Equal("first", () => Browser.FindElement(By.Id("test-5")).FindElement(By.CssSelector(".live-cached-0")).Text);
        Browser.Equal("second", () => Browser.FindElement(By.Id("test-5")).FindElement(By.CssSelector(".live-cached-1")).Text);
    }

    [Fact]
    public void CacheViewCachesHardcodedLiveCachedComponent()
    {
        Navigate(TestUrl("cache-component"));
        var panel = Browser.FindElement(By.Id("test-7"));
        var staticGuid = panel.FindElement(By.CssSelector(".panel-static")).Text;
        var liveCachedComponentGuid = panel.FindElement(By.CssSelector(".hardcoded-live-cached")).Text;
        Assert.NotEqual(staticGuid, liveCachedComponentGuid);

        // Warm reload: the wrapper's static output (emitted around the hardcoded live cached component) is served from
        // the cache, while the live cached component itself re-renders fresh on every request.
        Navigate(TestUrl("cache-component"));
        Browser.Equal(staticGuid, () => Browser.FindElement(By.Id("test-7")).FindElement(By.CssSelector(".panel-static")).Text);
        Browser.NotEqual(liveCachedComponentGuid, () => Browser.FindElement(By.Id("test-7")).FindElement(By.CssSelector(".hardcoded-live-cached")).Text);
    }

    [Fact]
    public void CacheViewTreatsStreamingChildAsLiveCachedComponent()
    {
        Navigate(TestUrl("cache-component"));
        // The streaming component is rendered via a streaming batch, so wait for it to arrive.
        var streamingGuid = Browser.Exists(By.CssSelector("#test-8 .streaming-live-cached")).Text;
        var staticGuid = Browser.FindElement(By.Id("test-8")).FindElement(By.CssSelector(".cached-static")).Text;
        Assert.NotEqual(staticGuid, streamingGuid);

        // Warm reload: the static content around the streaming component is served from the cache, while
        // the streaming component is treated as a live cached component and re-renders fresh on every request.
        Navigate(TestUrl("cache-component"));
        Browser.Equal(staticGuid, () => Browser.FindElement(By.Id("test-8")).FindElement(By.CssSelector(".cached-static")).Text);
        Browser.NotEqual(streamingGuid, () => Browser.FindElement(By.Id("test-8")).FindElement(By.CssSelector(".streaming-live-cached")).Text);
    }
}
