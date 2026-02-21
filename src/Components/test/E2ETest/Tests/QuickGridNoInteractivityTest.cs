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
        Assert.Contains("QuickGrid=2", Browser.Url);

        var grid = Browser.FindElement(By.ClassName("quickgrid"));
        var rowCount = grid.FindElements(By.CssSelector("tbody > tr")).Count;
        Assert.Equal(10, rowCount);
    }

    [Fact]
    public void PaginatorLinkLoadsCorrectPage()
    {
        Navigate($"{ServerPathBase}/quickgrid?QuickGrid=3");

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
        Navigate($"{ServerPathBase}/quickgrid?QuickGrid=999");

        Browser.Equal("5", () => Browser.FindElement(By.CssSelector(".first-paginator .paginator nav > div > strong:nth-child(1)")).Text);
    }

    [Fact]
    public void PaginatorInvalidPageValueDefaultsToFirstPage()
    {
        Navigate($"{ServerPathBase}/quickgrid?QuickGrid=abc");

        Browser.Equal("1", () => Browser.FindElement(By.CssSelector(".first-paginator .paginator nav > div > strong:nth-child(1)")).Text);
    }

    [Fact]
    public void CanColumnSortByInt()
    {
        Navigate($"{ServerPathBase}/quickgrid");

        Browser.FindElement(By.CssSelector("#grid table thead > tr > th:nth-child(1) button.col-title")).Click();
        Browser.Equal("10895", () => Browser.FindElement(By.CssSelector("#grid table tbody > tr:nth-child(1) > td:nth-child(1)")).Text);
        Assert.Contains("QuickGrid_s=0_asc", Browser.Url);
        Browser.FindElement(By.CssSelector("#grid table thead > tr > th:nth-child(1) button.col-title")).Click();

        Browser.Equal("12381", () => Browser.FindElement(By.CssSelector("#grid table tbody > tr:nth-child(1) > td:nth-child(1)")).Text);
        Assert.Contains("QuickGrid_s=0_desc", Browser.Url);

        var firstRow = Browser.FindElement(By.CssSelector("#grid table tbody > tr:nth-child(1)"));
        Assert.Equal("Matti", firstRow.FindElement(By.CssSelector("td:nth-child(2)")).Text);
        Assert.Equal("Karttunen", firstRow.FindElement(By.CssSelector("td:nth-child(3)")).Text);
        Assert.Equal("1981-06-04", firstRow.FindElement(By.CssSelector("td:nth-child(4)")).Text);
    }

    [Fact]
    public void SortLinkLoadsCorrectOrder()
    {
        Navigate($"{ServerPathBase}/quickgrid?QuickGrid_s=0_desc");

        Browser.Equal("12381", () => Browser.FindElement(By.CssSelector("#grid table tbody > tr:nth-child(1) > td:nth-child(1)")).Text);
        Assert.Equal("Matti", Browser.FindElement(By.CssSelector("#grid table tbody > tr:nth-child(1) > td:nth-child(2)")).Text);
    }

    [Fact]
    public void SortNavigationWithPagination()
    {
        Navigate($"{ServerPathBase}/quickgrid?QuickGrid=2");

        Browser.Equal("2", () => Browser.FindElement(By.CssSelector(".first-paginator .paginator nav > div > strong:nth-child(1)")).Text);
        Browser.FindElement(By.CssSelector("#grid table thead > tr > th:nth-child(1) button.col-title")).Click();
        Browser.True(() => Browser.Url.Contains("QuickGrid_s=0_asc"));
        var firstRow = Browser.FindElement(By.CssSelector("#grid table tbody > tr:nth-child(1)"));
        Assert.NotNull(firstRow);
    }

    [Fact]
    public void TwoGridsSortIndependently()
    {
        Navigate($"{ServerPathBase}/quickgrid");

        // Sort first grid by PersonId ascending
        Browser.FindElement(By.CssSelector("#grid table thead > tr > th:nth-child(1) button.col-title")).Click();
        Browser.Equal("10895", () => Browser.FindElement(By.CssSelector("#grid table tbody > tr:nth-child(1) > td:nth-child(1)")).Text);

        // Sort second grid by Name ascending
        Browser.FindElement(By.CssSelector("#grid2 table thead > tr > th:nth-child(2) button.col-title")).Click();
        Browser.Equal("Bangalore", () => Browser.FindElement(By.CssSelector("#grid2 table tbody > tr:nth-child(1) > td:nth-child(2)")).Text);
        Assert.Equal("10895", Browser.FindElement(By.CssSelector("#grid table tbody > tr:nth-child(1) > td:nth-child(1)")).Text);

        Assert.Contains("QuickGrid_s=0_asc", Browser.Url);
        Assert.Contains("QuickGrid2_s=1_asc", Browser.Url);

        // Sort second grid by Name descending
        Browser.FindElement(By.CssSelector("#grid2 table thead > tr > th:nth-child(2) button.col-title")).Click();
        Browser.Equal("Tokyo", () => Browser.FindElement(By.CssSelector("#grid2 table tbody > tr:nth-child(1) > td:nth-child(2)")).Text);

        // First grid should remain unchanged
        Assert.Equal("10895", Browser.FindElement(By.CssSelector("#grid table tbody > tr:nth-child(1) > td:nth-child(1)")).Text);
        Assert.Contains("QuickGrid_s=0_asc", Browser.Url);
        Assert.Contains("QuickGrid2_s=1_desc", Browser.Url);
    }
}
