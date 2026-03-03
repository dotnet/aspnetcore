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
    public void PaginatorNavigationLinksDisabledCorrectly()
    {
        Navigate($"{ServerPathBase}/quickgrid");

        Assert.Equal("true", Browser.FindElement(By.CssSelector(".first-paginator .go-first")).GetDomAttribute("aria-disabled"));
        Assert.Equal("true", Browser.FindElement(By.CssSelector(".first-paginator .go-previous")).GetDomAttribute("aria-disabled"));
        Assert.Equal("false", Browser.FindElement(By.CssSelector(".first-paginator .go-next")).GetDomAttribute("aria-disabled"));
        Assert.Equal("false", Browser.FindElement(By.CssSelector(".first-paginator .go-last")).GetDomAttribute("aria-disabled"));

        Browser.FindElement(By.CssSelector(".first-paginator .go-last")).Click();
        Browser.Equal("5", () => Browser.FindElement(By.CssSelector(".first-paginator .paginator nav > div > strong:nth-child(1)")).Text);

        Assert.Equal("false", Browser.FindElement(By.CssSelector(".first-paginator .go-first")).GetDomAttribute("aria-disabled"));
        Assert.Equal("false", Browser.FindElement(By.CssSelector(".first-paginator .go-previous")).GetDomAttribute("aria-disabled"));
        Assert.Equal("true", Browser.FindElement(By.CssSelector(".first-paginator .go-next")).GetDomAttribute("aria-disabled"));
        Assert.Equal("true", Browser.FindElement(By.CssSelector(".first-paginator .go-last")).GetDomAttribute("aria-disabled"));
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

    [Fact]
    public void CanColumnSortByInt()
    {
        Navigate($"{ServerPathBase}/quickgrid");

        Browser.FindElement(By.CssSelector("#grid table thead > tr > th:nth-child(1) a.col-title")).Click();
        Browser.Equal("10895", () => Browser.FindElement(By.CssSelector("#grid table tbody > tr:nth-child(1) > td:nth-child(1)")).Text);
        Assert.Contains("sort=PersonId", Browser.Url);
        Assert.Contains("order=asc", Browser.Url);
        Browser.FindElement(By.CssSelector("#grid table thead > tr > th:nth-child(1) a.col-title")).Click();

        Browser.Equal("12381", () => Browser.FindElement(By.CssSelector("#grid table tbody > tr:nth-child(1) > td:nth-child(1)")).Text);
        Assert.Contains("sort=PersonId", Browser.Url);
        Assert.Contains("order=desc", Browser.Url);

        var firstRow = Browser.FindElement(By.CssSelector("#grid table tbody > tr:nth-child(1)"));
        Assert.Equal("Matti", firstRow.FindElement(By.CssSelector("td:nth-child(2)")).Text);
        Assert.Equal("Karttunen", firstRow.FindElement(By.CssSelector("td:nth-child(3)")).Text);
        Assert.Equal("1981-06-04", firstRow.FindElement(By.CssSelector("td:nth-child(4)")).Text);
    }

    [Fact]
    public void SortLinkLoadsCorrectOrder()
    {
        Navigate($"{ServerPathBase}/quickgrid?sort=PersonId&order=desc");

        Browser.Equal("12381", () => Browser.FindElement(By.CssSelector("#grid table tbody > tr:nth-child(1) > td:nth-child(1)")).Text);
        Assert.Equal("Matti", Browser.FindElement(By.CssSelector("#grid table tbody > tr:nth-child(1) > td:nth-child(2)")).Text);
    }

    [Fact]
    public void SortNavigationWithPagination()
    {
        Navigate($"{ServerPathBase}/quickgrid?page=2");

        Browser.Equal("2", () => Browser.FindElement(By.CssSelector(".first-paginator .paginator nav > div > strong:nth-child(1)")).Text);
        Browser.FindElement(By.CssSelector("#grid table thead > tr > th:nth-child(1) a.col-title")).Click();
        Browser.True(() => Browser.Url.Contains("sort=PersonId"));
        Browser.True(() => Browser.Url.Contains("order=asc"));
        var firstRow = Browser.FindElement(By.CssSelector("#grid table tbody > tr:nth-child(1)"));
        Assert.NotNull(firstRow);
    }

    [Fact]
    public void TwoGridsSortIndependently()
    {
        Navigate($"{ServerPathBase}/quickgrid");

        // Sort first grid by PersonId ascending
        Browser.FindElement(By.CssSelector("#grid table thead > tr > th:nth-child(1) a.col-title")).Click();
        Browser.Equal("10895", () => Browser.FindElement(By.CssSelector("#grid table tbody > tr:nth-child(1) > td:nth-child(1)")).Text);

        // Sort second grid by Name ascending
        Browser.FindElement(By.CssSelector("#grid2 table thead > tr > th:nth-child(2) a.col-title")).Click();
        Browser.Equal("Bangalore", () => Browser.FindElement(By.CssSelector("#grid2 table tbody > tr:nth-child(1) > td:nth-child(2)")).Text);
        Assert.Equal("10895", Browser.FindElement(By.CssSelector("#grid table tbody > tr:nth-child(1) > td:nth-child(1)")).Text);

        Assert.Contains("sort=PersonId", Browser.Url);
        Assert.Contains("order=asc", Browser.Url);
        Assert.Contains("QuickGrid2_sort=Name", Browser.Url);
        Assert.Contains("QuickGrid2_order=asc", Browser.Url);

        // Sort second grid by Name descending
        Browser.FindElement(By.CssSelector("#grid2 table thead > tr > th:nth-child(2) a.col-title")).Click();
        Browser.Equal("Tokyo", () => Browser.FindElement(By.CssSelector("#grid2 table tbody > tr:nth-child(1) > td:nth-child(2)")).Text);

        // First grid should remain unchanged
        Assert.Equal("10895", Browser.FindElement(By.CssSelector("#grid table tbody > tr:nth-child(1) > td:nth-child(1)")).Text);
        Assert.Contains("sort=PersonId", Browser.Url);
        Assert.Contains("order=asc", Browser.Url);
        Assert.Contains("QuickGrid2_sort=Name", Browser.Url);
        Assert.Contains("QuickGrid2_order=desc", Browser.Url);
    }

    [Fact]
    public void DualPaginatorsNavigatesAndBothUpdate()
    {
        Navigate($"{ServerPathBase}/quickgrid");

        Browser.Equal("1", () => Browser.FindElement(By.CssSelector(".third-top-paginator .paginator nav > div > strong:nth-child(1)")).Text);
        Browser.Equal("1", () => Browser.FindElement(By.CssSelector(".third-bottom-paginator .paginator nav > div > strong:nth-child(1)")).Text);

        Browser.FindElement(By.CssSelector(".third-top-paginator .go-next")).Click();

        Browser.Equal("2", () => Browser.FindElement(By.CssSelector(".third-top-paginator .paginator nav > div > strong:nth-child(1)")).Text);
        Browser.Equal("2", () => Browser.FindElement(By.CssSelector(".third-bottom-paginator .paginator nav > div > strong:nth-child(1)")).Text);
    }
}
