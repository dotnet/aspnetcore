// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components.E2ETest.Infrastructure;
using Microsoft.AspNetCore.Components.E2ETest.Infrastructure.ServerFixtures;
using Microsoft.AspNetCore.E2ETesting;
using OpenQA.Selenium;
using Xunit.Abstractions;

namespace Microsoft.AspNetCore.Components.E2ETest.Tests;

public class StandaloneAppTest
    : ServerTestBase<BlazorWasmTestAppFixture<StandaloneApp.Program>>, IDisposable
{
    public StandaloneAppTest(
        BrowserFixture browserFixture,
        BlazorWasmTestAppFixture<StandaloneApp.Program> serverFixture,
        ITestOutputHelper output)
        : base(browserFixture, serverFixture, output)
    {
    }

    protected override void InitializeAsyncCore()
    {
        Navigate("/");
        WaitUntilLoaded();
    }

    [Fact]
    public void HasTitle()
    {
        Assert.Equal("Blazor standalone", Browser.Title);
    }

    [Fact]
    public void HasHeading()
    {
        Assert.Equal("Hello, world!", Browser.Exists(By.TagName("h1")).Text);
    }

    [Fact]
    public void NavMenuHighlightsCurrentLocation()
    {
        var activeNavLinksSelector = By.CssSelector(".sidebar a.active");
        var mainHeaderSelector = By.TagName("h1");

        // Verify we start at home, with the home link highlighted
        Assert.Equal("Hello, world!", Browser.Exists(mainHeaderSelector).Text);
        Assert.Collection(Browser.FindElements(activeNavLinksSelector),
            item => Assert.Equal("Home", item.Text.Trim()));

        // Click on the "counter" link
        Browser.Exists(By.LinkText("Counter")).Click();

        // Verify we're now on the counter page, with that nav link (only) highlighted
        Assert.Equal("Counter", Browser.Exists(mainHeaderSelector).Text);
        Assert.Collection(Browser.FindElements(activeNavLinksSelector),
            item => Assert.Equal("Counter", item.Text.Trim()));

        // Verify we can navigate back to home too
        Browser.Exists(By.LinkText("Home")).Click();
        Assert.Equal("Hello, world!", Browser.Exists(mainHeaderSelector).Text);
        Assert.Collection(Browser.FindElements(activeNavLinksSelector),
            item => Assert.Equal("Home", item.Text.Trim()));
    }

    [Fact]
    public void HasCounterPage()
    {
        // Navigate to "Counter"
        Browser.Exists(By.LinkText("Counter")).Click();
        Assert.Equal("Counter", Browser.Exists(By.TagName("h1")).Text);

        // Observe the initial value is zero
        var countDisplayElement = Browser.Exists(By.CssSelector("h1 + p"));
        Assert.Equal("Current count: 0", countDisplayElement.Text);

        // Click the button; see it counts
        var button = Browser.Exists(By.CssSelector(".main button"));
        button.Click();
        button.Click();
        button.Click();
        Assert.Equal("Current count: 3", countDisplayElement.Text);
    }

    [Fact]
    public void HasFetchDataPage()
    {
        // Navigate to "Fetch data"
        Browser.Exists(By.LinkText("Fetch data")).Click();
        Assert.Equal("Weather forecast", Browser.Exists(By.TagName("h1")).Text);

        // Wait until loaded
        var tableSelector = By.CssSelector("table.table");
        Browser.Exists(tableSelector);

        // Check the table is displayed correctly
        var rows = Browser.FindElements(By.CssSelector("table.table tbody tr"));
        Assert.Equal(5, rows.Count);
        var cells = rows.SelectMany(row => row.FindElements(By.TagName("td")));
        foreach (var cell in cells)
        {
            Assert.True(!string.IsNullOrEmpty(cell.Text));
        }
    }

    [Fact]
    public void IsStarted()
    {
        // Read from property
        var jsExecutor = (IJavaScriptExecutor)Browser;

        var isStarted = jsExecutor.ExecuteScript("return window['__aspnetcore__testing__blazor_wasm__started__'];");
        if (isStarted is null)
        {
            throw new InvalidOperationException("Blazor wasm started value not set");
        }

        // Confirm server has started
        Assert.True((bool)isStarted);
    }

    private void WaitUntilLoaded()
    {
        var app = Browser.Exists(By.TagName("app"));
        Browser.NotEqual("Loading...", () => app.Text);
    }

    public void Dispose()
    {
        // Make the tests run faster by navigating back to the home page when we are done
        // If we don't, then the next test will reload the whole page before it starts
        Browser.Exists(By.LinkText("Home")).Click();
    }
}
