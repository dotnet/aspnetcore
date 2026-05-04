// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Components.TestServer.RazorComponents;
using Microsoft.AspNetCore.Components.E2ETest.Infrastructure;
using Microsoft.AspNetCore.Components.E2ETest.Infrastructure.ServerFixtures;
using Microsoft.AspNetCore.E2ETesting;
using OpenQA.Selenium;
using TestServer;
using Xunit.Abstractions;

namespace Microsoft.AspNetCore.Components.E2ETests.Tests;

public class QuickGridInteractiveTest : ServerTestBase<BasicTestAppServerSiteFixture<RazorComponentEndpointsStartup<App>>>
{
    public QuickGridInteractiveTest(
        BrowserFixture browserFixture,
        BasicTestAppServerSiteFixture<RazorComponentEndpointsStartup<App>> serverFixture,
        ITestOutputHelper output)
        : base(browserFixture, serverFixture, output)
    {
    }

    public override Task InitializeAsync() => InitializeAsync(BrowserFixture.StreamingContext);

    [Fact]
    public void CanColumnSortByInt()
    {
        Navigate($"{ServerPathBase}/quickgrid-interactive");
        Browser.Exists(By.CssSelector("#grid > table"));

        // Click to sort ascending, wait for result
        Browser.Click(By.CssSelector("#grid > table thead > tr > th:nth-child(1) > div > a"));
        Browser.Equal("10895", () => Browser.FindElement(By.CssSelector("#grid > table tbody > tr:nth-child(1) > td:nth-child(1)")).Text);
        Assert.Contains("sort=PersonId", Browser.Url);
        Assert.Contains("order=asc", Browser.Url);

        // Click again to sort descending
        Browser.Click(By.CssSelector("#grid > table thead > tr > th:nth-child(1) > div > a"));

        //Compare first row to expected result
        Browser.Equal("12381", () => Browser.FindElement(By.CssSelector("#grid > table tbody > tr:nth-child(1) > td:nth-child(1)")).Text);
        Browser.Equal("Matti", () => Browser.FindElement(By.CssSelector("#grid > table tbody > tr:nth-child(1) > td:nth-child(2)")).Text);
        Browser.Equal("Karttunen", () => Browser.FindElement(By.CssSelector("#grid > table tbody > tr:nth-child(1) > td:nth-child(3)")).Text);
        Browser.Equal("1981-06-04", () => Browser.FindElement(By.CssSelector("#grid > table tbody > tr:nth-child(1) > td:nth-child(4)")).Text);
        Assert.Contains("sort=PersonId", Browser.Url);
        Assert.Contains("order=desc", Browser.Url);
    }

    [Fact]
    public void CanColumnSortByString()
    {
        Navigate($"{ServerPathBase}/quickgrid-interactive");
        Browser.Exists(By.CssSelector("#grid > table"));

        // Click to sort ascending, wait for result
        Browser.Click(By.CssSelector("#grid > table thead > tr > th:nth-child(2) > div > a.col-title"));
        Browser.Equal("12372", () => Browser.FindElement(By.CssSelector("#grid > table tbody > tr:nth-child(1) > td:nth-child(1)")).Text);
        Assert.Contains("sort=FirstName", Browser.Url);
        Assert.Contains("order=asc", Browser.Url);

        // Click again to sort descending
        Browser.Click(By.CssSelector("#grid > table thead > tr > th:nth-child(2) > div > a.col-title"));

        //Compare first row to expected result
        Browser.Equal("12379", () => Browser.FindElement(By.CssSelector("#grid > table tbody > tr:nth-child(1) > td:nth-child(1)")).Text);
        Browser.Equal("Zbyszek", () => Browser.FindElement(By.CssSelector("#grid > table tbody > tr:nth-child(1) > td:nth-child(2)")).Text);
        Browser.Equal("Piestrzeniewicz", () => Browser.FindElement(By.CssSelector("#grid > table tbody > tr:nth-child(1) > td:nth-child(3)")).Text);
        Browser.Equal("1981-04-02", () => Browser.FindElement(By.CssSelector("#grid > table tbody > tr:nth-child(1) > td:nth-child(4)")).Text);
        Assert.Contains("sort=FirstName", Browser.Url);
        Assert.Contains("order=desc", Browser.Url);
    }

    [Fact]
    public void CanColumnSortByDateOnly()
    {
        Navigate($"{ServerPathBase}/quickgrid-interactive");
        Browser.Exists(By.CssSelector("#grid > table"));

        // Click to sort ascending, wait for result
        Browser.Click(By.CssSelector("#grid > table thead > tr > th:nth-child(4) > div > a"));
        Browser.Equal("11205", () => Browser.FindElement(By.CssSelector("#grid > table tbody > tr:nth-child(1) > td:nth-child(1)")).Text);
        Assert.Contains("sort=BirthDate", Browser.Url);
        Assert.Contains("order=asc", Browser.Url);

        // Click again to sort descending
        Browser.Click(By.CssSelector("#grid > table thead > tr > th:nth-child(4) > div > a"));

        //Compare first row to expected result
        Browser.Equal("12364", () => Browser.FindElement(By.CssSelector("#grid > table tbody > tr:nth-child(1) > td:nth-child(1)")).Text);
        Browser.Equal("Paolo", () => Browser.FindElement(By.CssSelector("#grid > table tbody > tr:nth-child(1) > td:nth-child(2)")).Text);
        Browser.Equal("Accorti", () => Browser.FindElement(By.CssSelector("#grid > table tbody > tr:nth-child(1) > td:nth-child(3)")).Text);
        Browser.Equal("2018-05-18", () => Browser.FindElement(By.CssSelector("#grid > table tbody > tr:nth-child(1) > td:nth-child(4)")).Text);
        Assert.Contains("sort=BirthDate", Browser.Url);
        Assert.Contains("order=desc", Browser.Url);
    }

    [Fact]
    public void PaginatorCorrectItemsPerPage()
    {
        Navigate($"{ServerPathBase}/quickgrid-interactive");
        Browser.Exists(By.CssSelector("#grid > table"));

        var grid = Browser.FindElement(By.ClassName("quickgrid"));
        var rowCount = grid.FindElements(By.CssSelector("tbody > tr")).Count;
        Assert.Equal(10, rowCount);

        Browser.Click(By.CssSelector(".first-paginator .go-next"));

        Browser.Equal("2", () => Browser.FindElement(By.CssSelector(".first-paginator .paginator nav > div > strong:nth-child(1)")).Text);
        Browser.Equal(10, () => Browser.FindElement(By.CssSelector("#grid .quickgrid")).FindElements(By.CssSelector("tbody > tr")).Count);
    }

    [Fact]
    public void DualPaginatorsNavigatesAndBothUpdate()
    {
        Navigate($"{ServerPathBase}/quickgrid-interactive");
        Browser.Exists(By.CssSelector("#grid2 > table"));

        Browser.Equal("1", () => Browser.FindElement(By.CssSelector(".top-paginator .paginator nav > div > strong:nth-child(1)")).Text);
        Browser.Equal("1", () => Browser.FindElement(By.CssSelector(".bottom-paginator .paginator nav > div > strong:nth-child(1)")).Text);

        Browser.Click(By.CssSelector(".top-paginator .go-next"));

        Browser.Equal("2", () => Browser.FindElement(By.CssSelector(".top-paginator .paginator nav > div > strong:nth-child(1)")).Text);
        Browser.Equal("2", () => Browser.FindElement(By.CssSelector(".bottom-paginator .paginator nav > div > strong:nth-child(1)")).Text);
        Assert.Contains("people_page=2", Browser.Url);
    }

    [Fact]
    public void TwoGridsSortIndependently()
    {
        Navigate($"{ServerPathBase}/quickgrid-interactive");
        Browser.Exists(By.CssSelector("#grid > table"));
        Browser.Exists(By.CssSelector("#grid2 > table"));

        // Sort first grid by PersonId ascending
        Browser.Click(By.CssSelector("#grid > table thead > tr > th:nth-child(1) > div > a"));
        Browser.Equal("10895", () => Browser.FindElement(By.CssSelector("#grid > table tbody > tr:nth-child(1) > td:nth-child(1)")).Text);

        // Sort second grid by FirstName ascending (uses QueryParameterNamePrefix="people" prefix)
        Browser.Click(By.CssSelector("#grid2 > table thead > tr > th:nth-child(2) > div > a.col-title"));
        Browser.Equal("12372", () => Browser.FindElement(By.CssSelector("#grid2 > table tbody > tr:nth-child(1) > td:nth-child(1)")).Text);

        // First grid should remain sorted
        Browser.Equal("10895", () => Browser.FindElement(By.CssSelector("#grid > table tbody > tr:nth-child(1) > td:nth-child(1)")).Text);

        // Verify URL contains both unprefixed and prefixed sort parameters
        Assert.Contains("sort=PersonId", Browser.Url);
        Assert.Contains("order=asc", Browser.Url);
        Assert.Contains("people_sort=FirstName", Browser.Url);
        Assert.Contains("people_order=asc", Browser.Url);
    }
}
