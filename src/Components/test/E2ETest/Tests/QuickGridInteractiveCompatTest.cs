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

/// <summary>
/// Tests QuickGrid with URL-driven navigation disabled via the
/// <c>Microsoft.AspNetCore.Components.QuickGrid.EnableUrlBasedQuickGridNavigationAndSorting</c> AppContext switch.
/// Mirrors <see cref="QuickGridInteractiveTest"/> but asserts button-based UI elements are rendered.
/// URL still updates in both modes — only the rendered HTML element (button vs anchor) differs.
/// </summary>
public class QuickGridInteractiveCompatTest : ServerTestBase<BasicTestAppServerSiteFixture<RazorComponentEndpointsStartup<App>>>
{
    public QuickGridInteractiveCompatTest(
        BrowserFixture browserFixture,
        BasicTestAppServerSiteFixture<RazorComponentEndpointsStartup<App>> serverFixture,
        ITestOutputHelper output)
        : base(browserFixture, serverFixture, output)
    {
    }

    protected override void InitializeAsyncCore()
    {
        _serverFixture.AdditionalArguments.Add("--DisableUrlDrivenNavigation=true");
        base.InitializeAsyncCore();
    }

    public override Task InitializeAsync() => InitializeAsync(BrowserFixture.StreamingContext);

    [Fact]
    public void CanColumnSortByInt()
    {
        Navigate($"{ServerPathBase}/quickgrid-interactive");
        Browser.Exists(By.CssSelector("#grid > table"));

        // Click button to sort ascending
        Browser.Click(By.CssSelector("#grid > table thead > tr > th:nth-child(1) > div > button.col-title"));
        Browser.Equal("10895", () => Browser.FindElement(By.CssSelector("#grid > table tbody > tr:nth-child(1) > td:nth-child(1)")).Text);

        // URL updates in both modes; only the rendered element differs
        Assert.Contains("sort=PersonId", Browser.Url);
        Assert.Contains("direction=asc", Browser.Url);

        // Click again to sort descending
        Browser.Click(By.CssSelector("#grid > table thead > tr > th:nth-child(1) > div > button.col-title"));

        Browser.Equal("12381", () => Browser.FindElement(By.CssSelector("#grid > table tbody > tr:nth-child(1) > td:nth-child(1)")).Text);
        Browser.Equal("Matti", () => Browser.FindElement(By.CssSelector("#grid > table tbody > tr:nth-child(1) > td:nth-child(2)")).Text);
        Browser.Equal("Karttunen", () => Browser.FindElement(By.CssSelector("#grid > table tbody > tr:nth-child(1) > td:nth-child(3)")).Text);
        Browser.Equal("1981-06-04", () => Browser.FindElement(By.CssSelector("#grid > table tbody > tr:nth-child(1) > td:nth-child(4)")).Text);

        Assert.Contains("sort=PersonId", Browser.Url);
        Assert.Contains("direction=desc", Browser.Url);
    }

    [Fact]
    public void CanColumnSortByString()
    {
        Navigate($"{ServerPathBase}/quickgrid-interactive");
        Browser.Exists(By.CssSelector("#grid > table"));

        // Click button to sort ascending
        Browser.Click(By.CssSelector("#grid > table thead > tr > th:nth-child(2) > div > button.col-title"));
        Browser.Equal("12372", () => Browser.FindElement(By.CssSelector("#grid > table tbody > tr:nth-child(1) > td:nth-child(1)")).Text);
        Assert.Contains("sort=FirstName", Browser.Url);
        Assert.Contains("direction=asc", Browser.Url);

        // Click again to sort descending
        Browser.Click(By.CssSelector("#grid > table thead > tr > th:nth-child(2) > div > button.col-title"));

        Browser.Equal("12379", () => Browser.FindElement(By.CssSelector("#grid > table tbody > tr:nth-child(1) > td:nth-child(1)")).Text);
        Browser.Equal("Zbyszek", () => Browser.FindElement(By.CssSelector("#grid > table tbody > tr:nth-child(1) > td:nth-child(2)")).Text);
        Browser.Equal("Piestrzeniewicz", () => Browser.FindElement(By.CssSelector("#grid > table tbody > tr:nth-child(1) > td:nth-child(3)")).Text);
        Browser.Equal("1981-04-02", () => Browser.FindElement(By.CssSelector("#grid > table tbody > tr:nth-child(1) > td:nth-child(4)")).Text);
        Assert.Contains("sort=FirstName", Browser.Url);
        Assert.Contains("direction=desc", Browser.Url);
    }

    [Fact]
    public void CanColumnSortByDateOnly()
    {
        Navigate($"{ServerPathBase}/quickgrid-interactive");
        Browser.Exists(By.CssSelector("#grid > table"));

        // Click button to sort ascending
        Browser.Click(By.CssSelector("#grid > table thead > tr > th:nth-child(4) > div > button.col-title"));
        Browser.Equal("11205", () => Browser.FindElement(By.CssSelector("#grid > table tbody > tr:nth-child(1) > td:nth-child(1)")).Text);
        Assert.Contains("sort=BirthDate", Browser.Url);
        Assert.Contains("direction=asc", Browser.Url);

        // Click again to sort descending
        Browser.Click(By.CssSelector("#grid > table thead > tr > th:nth-child(4) > div > button.col-title"));

        Browser.Equal("12364", () => Browser.FindElement(By.CssSelector("#grid > table tbody > tr:nth-child(1) > td:nth-child(1)")).Text);
        Browser.Equal("Paolo", () => Browser.FindElement(By.CssSelector("#grid > table tbody > tr:nth-child(1) > td:nth-child(2)")).Text);
        Browser.Equal("Accorti", () => Browser.FindElement(By.CssSelector("#grid > table tbody > tr:nth-child(1) > td:nth-child(3)")).Text);
        Browser.Equal("2018-05-18", () => Browser.FindElement(By.CssSelector("#grid > table tbody > tr:nth-child(1) > td:nth-child(4)")).Text);
        Assert.Contains("sort=BirthDate", Browser.Url);
        Assert.Contains("direction=desc", Browser.Url);
    }

    [Fact]
    public void PaginatorCorrectItemsPerPage()
    {
        Navigate($"{ServerPathBase}/quickgrid-interactive");
        Browser.Exists(By.CssSelector("#grid > table"));

        Browser.Equal(10, () => Browser.FindElements(By.CssSelector("#grid .quickgrid tbody tr")).Count);

        Browser.Click(By.CssSelector(".first-paginator button.go-next"));

        Browser.Equal("2", () => Browser.FindElement(By.CssSelector(".first-paginator .paginator nav > div > strong:nth-child(1)")).Text);
        Browser.Equal(10, () => Browser.FindElements(By.CssSelector("#grid .quickgrid tbody tr")).Count);

        // URL updates in both modes
        Browser.Contains("page=2", () => Browser.Url);
    }

    [Fact]
    public void TwoGridsSortIndependently()
    {
        Navigate($"{ServerPathBase}/quickgrid-interactive");
        Browser.Exists(By.CssSelector("#grid > table"));
        Browser.Exists(By.CssSelector("#grid2 > table"));

        // Sort first grid by PersonId ascending (button, not anchor)
        Browser.Click(By.CssSelector("#grid > table thead > tr > th:nth-child(1) > div > button.col-title"));
        Browser.Equal("10895", () => Browser.FindElement(By.CssSelector("#grid > table tbody > tr:nth-child(1) > td:nth-child(1)")).Text);

        // Sort second grid by FirstName ascending (button, not anchor)
        Browser.Click(By.CssSelector("#grid2 > table thead > tr > th:nth-child(2) > div > button.col-title"));
        Browser.Equal("12372", () => Browser.FindElement(By.CssSelector("#grid2 > table tbody > tr:nth-child(1) > td:nth-child(1)")).Text);

        // First grid should remain sorted
        Browser.Equal("10895", () => Browser.FindElement(By.CssSelector("#grid > table tbody > tr:nth-child(1) > td:nth-child(1)")).Text);

        // URL updates in both modes
        Assert.Contains("sort=PersonId", Browser.Url);
        Assert.Contains("direction=asc", Browser.Url);
    }

    [Theory]
    [InlineData("first-paginator", "5", "43 items")]
    [InlineData("bottom-paginator", "9", "Custom summary")]
    public void PaginatorExposesPageChangesWithoutMovingFocus(string paginatorClass, string pageCount, string summaryText)
    {
        Navigate($"{ServerPathBase}/quickgrid-interactive");
        Browser.Exists(By.Id("quickgrid-interactive-ready"));

        var paginator = Browser.FindElement(By.ClassName(paginatorClass));
        var status = paginator.FindElement(By.ClassName("pagination-text"));
        var summary = paginator.FindElement(By.ClassName("summary"));
        var next = paginator.FindElement(By.CssSelector("button.go-next"));

        Assert.Equal($"Page 1 of {pageCount}", status.Text);
        Assert.Equal("polite", status.GetDomAttribute("aria-live"));
        Assert.Equal("true", status.GetDomAttribute("aria-atomic"));
        Assert.Null(summary.GetDomAttribute("aria-live"));

        next.SendKeys(Keys.Enter);

        Browser.Equal($"Page 2 of {pageCount}", () => status.Text);
        Browser.Equal(next, () => Browser.SwitchTo().ActiveElement());
        Assert.Equal("polite", status.GetDomAttribute("aria-live"));
        Assert.Equal("true", status.GetDomAttribute("aria-atomic"));
        Assert.Equal(summaryText, summary.Text);
    }
}
