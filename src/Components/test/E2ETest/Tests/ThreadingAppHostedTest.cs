// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components.E2ETest.Infrastructure;
using Microsoft.AspNetCore.Components.E2ETest.Infrastructure.ServerFixtures;
using Microsoft.AspNetCore.E2ETesting;
using Microsoft.AspNetCore.InternalTesting;
using OpenQA.Selenium;
using Xunit.Abstractions;

namespace Microsoft.AspNetCore.Components.E2ETest.Tests;

public class ThreadingHostedAppTest
    : ServerTestBase<ThreadingHostedAppTest.ThreadingAppServerSiteFixture>
{
    public class ThreadingAppServerSiteFixture : AspNetSiteServerFixture
    {
        public ThreadingAppServerSiteFixture()
        {
            BuildWebHostMethod = ThreadingApp.Server.Program.BuildWebHost;
        }
    }

    public ThreadingHostedAppTest(BrowserFixture browserFixture, ThreadingAppServerSiteFixture serverFixture, ITestOutputHelper output)
        : base(browserFixture, serverFixture, output)
    {
    }

    protected override void InitializeAsyncCore()
    {
        Navigate("/");
        WaitUntilLoaded();
    }

    [Fact(Skip = "https://github.com/dotnet/aspnetcore/issues/63524")]
    public void HasHeading()
    {
        Assert.Equal("Hello, world!", Browser.Exists(By.TagName("h1")).Text);
    }

    [Fact(Skip = "https://github.com/dotnet/aspnetcore/issues/63524")]
    public void NavMenuHighlightsCurrentLocation()
    {
        var activeNavLinksSelector = By.CssSelector(".sidebar a.active");
        var mainHeaderSelector = By.TagName("h1");

        // Verify we start at home, with the home link highlighted
        Browser.Equal("Hello, world!", () => Browser.Exists(mainHeaderSelector).Text);
        Browser.Collection(() => Browser.FindElements(activeNavLinksSelector),
            item => Assert.Equal("Home", item.Text.Trim()));

        // Click on the "counter" link
        Browser.Exists(By.LinkText("Counter")).Click();

        // Verify we're now on the counter page, with that nav link (only) highlighted
        Browser.Equal("Counter", () => Browser.Exists(mainHeaderSelector).Text);
        Browser.Collection(() => Browser.FindElements(activeNavLinksSelector),
            item => Assert.Equal("Counter", item.Text.Trim()));

        // Verify we can navigate back to home too
        Browser.Exists(By.LinkText("Home")).Click();
        Browser.Equal("Hello, world!", () => Browser.Exists(mainHeaderSelector).Text);
        Browser.Collection(() => Browser.FindElements(activeNavLinksSelector),
            item => Assert.Equal("Home", item.Text.Trim()));
    }

    [Fact(Skip = "https://github.com/dotnet/aspnetcore/issues/63524")]
    public void CounterPageCanUseThreads()
    {
        // Navigate to "Counter"
        Browser.Exists(By.LinkText("Counter")).Click();
        Browser.Equal("Counter", () => Browser.Exists(By.TagName("h1")).Text);

        // see that initial state is zero
        Browser.Equal("Current count: 0", () => Browser.Exists(By.CssSelector("h1 + p")).Text);

        // start the test
        Browser.Exists(By.Id("TestThreads")).Click();

        // wait and see timer increase
        Browser.NotEqual("Current count: 0", () => Browser.Exists(By.CssSelector("h1 + p")).Text);
    }

    [Fact(Skip = "https://github.com/dotnet/aspnetcore/issues/63524")]
    public void HasFetchDataPage()
    {
        // Navigate to "Fetch data"
        Browser.Exists(By.LinkText("Fetch data")).Click();
        Browser.Equal("Weather forecast", () => Browser.Exists(By.TagName("h1")).Text);

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

    private void WaitUntilLoaded()
    {
        var app = Browser.Exists(By.TagName("app"));
        Browser.NotEqual("Loading...", () => app.Text);
    }
}
