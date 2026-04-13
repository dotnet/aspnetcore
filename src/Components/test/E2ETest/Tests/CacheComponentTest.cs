// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Components.TestServer.RazorComponents;
using Microsoft.AspNetCore.Components.E2ETest.Infrastructure;
using Microsoft.AspNetCore.Components.E2ETest.Infrastructure.ServerFixtures;
using Microsoft.AspNetCore.E2ETesting;
using OpenQA.Selenium;
using TestServer;
using Xunit.Abstractions;

namespace Microsoft.AspNetCore.Components.E2ETest.Tests;

public class CacheComponentTest : ServerTestBase<BasicTestAppServerSiteFixture<RazorComponentEndpointsNoInteractivityStartup<App>>>
{
    public CacheComponentTest(
        BrowserFixture browserFixture,
        BasicTestAppServerSiteFixture<RazorComponentEndpointsNoInteractivityStartup<App>> serverFixture,
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
    public void CacheComponentCachesData()
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
    public void CacheComponentDoesNotCacheDataWhenNotEnabled()
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
    public void CacheComponentCorrectlyCreatesHoles()
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
    public void NestedCacheComponentDoesNotExecuteOnOuterCacheHit()
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
    public void CacheComponentInLoopUsesVaryByForDistinctEntries()
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
    public void CacheComponentCachesLoopContent()
    {
        Navigate($"{ServerPathBase}/cache-component");
        var items = Browser.FindElement(By.Id("test-6")).FindElements(By.CssSelector(".loop-cached-item"));
        Assert.Equal(3, items.Count);

        var firstRenderValues = new string[3];
        for (var i = 0; i < 3; i++)
        {
            firstRenderValues[i] = items[i].Text;
        }

        // Second navigation — all loop items should come from cache
        Navigate($"{ServerPathBase}/cache-component");
        for (var i = 0; i < 3; i++)
        {
            var index = i;
            Browser.Equal(firstRenderValues[index], () =>
                Browser.FindElement(By.Id("test-6"))
                    .FindElements(By.CssSelector(".loop-cached-item"))[index].Text);
        }
    }

    private int GetRenderCount()
    {
        Navigate($"{ServerPathBase}/cache-component/render-count");
        var body = Browser.FindElement(By.TagName("body")).Text;
        return int.Parse(body, CultureInfo.InvariantCulture);
    }
}
