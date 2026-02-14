// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components.E2ETest.Infrastructure;
using Microsoft.AspNetCore.Components.E2ETest.Infrastructure.ServerFixtures;
using Components.TestServer.RazorComponents;
using Microsoft.AspNetCore.E2ETesting;
using Xunit.Abstractions;
using OpenQA.Selenium;
using TestServer;

namespace Microsoft.AspNetCore.Components.E2ETest.Tests;

public class QuickGridNoInteractivityTest : ServerTestBase<BasicTestAppServerSiteFixture<RazorComponentEndpointsNoInteractivityStartup<App>>>
{
    public QuickGridNoInteractivityTest(
        BrowserFixture browserFixture,
        BasicTestAppServerSiteFixture<RazorComponentEndpointsNoInteractivityStartup<App>> serverFixture,
        ITestOutputHelper output)
        : base(browserFixture, serverFixture, output)
    {
    }

    public override Task InitializeAsync() => InitializeAsync(BrowserFixture.StreamingContext);

    [Fact]
    public void PaginatorDisplaysCorrectItemCount()
    {
        Navigate($"{ServerPathBase}/quickgrid");

        var paginator = Browser.FindElement(By.CssSelector(".first-paginator .paginator"));

        var paginatorCount = paginator.FindElement(By.CssSelector("div > strong")).Text;
        var currentPageNumber = paginator.FindElement(By.CssSelector("nav > div > strong:nth-child(1)")).Text;
        var totalPageNumber = paginator.FindElement(By.CssSelector("nav > div > strong:nth-child(2)")).Text;

        Assert.Equal("43", paginatorCount);
        Assert.Equal("1", currentPageNumber);
        Assert.Equal("5", totalPageNumber);
    }

    [Fact]
    public void PaginatorGoNextShowsNextPage()
    {
        Navigate($"{ServerPathBase}/quickgrid");

        Browser.FindElement(By.CssSelector(".first-paginator .go-next")).Click();

        Browser.Equal("2", () => Browser.FindElement(By.CssSelector(".first-paginator .paginator nav > div > strong:nth-child(1)")).Text);
        Assert.Contains("page=2", Browser.Url);

        var grid = Browser.FindElement(By.ClassName("quickgrid"));
        var rowCount = grid.FindElements(By.CssSelector("tbody > tr")).Count;
        Assert.Equal(10, rowCount);
    }

    [Fact]
    public void PaginatorLinkLoadsCorrectPage()
    {
        Navigate($"{ServerPathBase}/quickgrid?page=3");

        Browser.Equal("3", () => Browser.FindElement(By.CssSelector(".first-paginator .paginator nav > div > strong:nth-child(1)")).Text);
    }

    [Fact]
    public void PaginatorGoPreviousFromSecondPage()
    {
        Navigate($"{ServerPathBase}/quickgrid");

        Browser.FindElement(By.CssSelector(".first-paginator .go-next")).Click();
        Browser.Equal("2", () => Browser.FindElement(By.CssSelector(".first-paginator .paginator nav > div > strong:nth-child(1)")).Text);

        Browser.FindElement(By.CssSelector(".first-paginator .go-previous")).Click();
        Browser.Equal("1", () => Browser.FindElement(By.CssSelector(".first-paginator .paginator nav > div > strong:nth-child(1)")).Text);
    }

    [Fact]
    public void PaginatorNavigationButtonsDisabledCorrectly()
    {
        Navigate($"{ServerPathBase}/quickgrid");

        Assert.Equal("true", Browser.FindElement(By.CssSelector(".first-paginator .go-first")).GetDomAttribute("disabled"));
        Assert.Equal("true", Browser.FindElement(By.CssSelector(".first-paginator .go-previous")).GetDomAttribute("disabled"));
        Assert.Null(Browser.FindElement(By.CssSelector(".first-paginator .go-next")).GetDomAttribute("disabled"));
        Assert.Null(Browser.FindElement(By.CssSelector(".first-paginator .go-last")).GetDomAttribute("disabled"));

        Browser.FindElement(By.CssSelector(".first-paginator .go-last")).Click();
        Browser.Equal("5", () => Browser.FindElement(By.CssSelector(".first-paginator .paginator nav > div > strong:nth-child(1)")).Text);

        Assert.Null(Browser.FindElement(By.CssSelector(".first-paginator .go-first")).GetDomAttribute("disabled"));
        Assert.Null(Browser.FindElement(By.CssSelector(".first-paginator .go-previous")).GetDomAttribute("disabled"));
        Assert.Equal("true", Browser.FindElement(By.CssSelector(".first-paginator .go-next")).GetDomAttribute("disabled"));
        Assert.Equal("true", Browser.FindElement(By.CssSelector(".first-paginator .go-last")).GetDomAttribute("disabled"));
    }

    [Fact]
    public void MultiplePaginatorsWorkIndependently()
    {
        Navigate($"{ServerPathBase}/quickgrid");
        Browser.FindElement(By.CssSelector(".second-paginator .go-next")).Click();

        Browser.Equal("1", () => Browser.FindElement(By.CssSelector(".first-paginator .paginator nav > div > strong:nth-child(1)")).Text);
        Browser.Equal("2", () => Browser.FindElement(By.CssSelector(".second-paginator .paginator nav > div > strong:nth-child(1)")).Text);

        var grid1 = Browser.FindElement(By.CssSelector("#grid .quickgrid"));
        Assert.Equal(10, grid1.FindElements(By.CssSelector("tbody > tr")).Count);
        var grid2 = Browser.FindElement(By.CssSelector("#grid2 .quickgrid"));
        Assert.Equal(5, grid2.FindElements(By.CssSelector("tbody > tr")).Count);
    }

    [Fact]
    public void PaginatorOutOfRangePageClampsToLastPage()
    {
        Navigate($"{ServerPathBase}/quickgrid?page=999");

        Browser.Equal("5", () => Browser.FindElement(By.CssSelector(".first-paginator .paginator nav > div > strong:nth-child(1)")).Text);
    }

    [Fact]
    public void PaginatorInvalidPageValueDefaultsToFirstPage()
    {
        Navigate($"{ServerPathBase}/quickgrid?page=abc");

        Browser.Equal("1", () => Browser.FindElement(By.CssSelector(".first-paginator .paginator nav > div > strong:nth-child(1)")).Text);
    }
}
