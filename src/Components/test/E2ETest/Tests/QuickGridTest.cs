// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using BasicTestApp;
using BasicTestApp.QuickGridTest;
using Microsoft.AspNetCore.Components.E2ETest;
using Microsoft.AspNetCore.Components.E2ETest.Infrastructure;
using Microsoft.AspNetCore.Components.E2ETest.Infrastructure.ServerFixtures;
using Microsoft.AspNetCore.E2ETesting;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.Extensions;
using Xunit.Abstractions;

namespace Microsoft.AspNetCore.Components.E2ETests.Tests;

public class QuickGridTest : ServerTestBase<ToggleExecutionModeServerFixture<Program>>
{
    protected IWebElement app;

    public QuickGridTest(
        BrowserFixture browserFixture,
        ToggleExecutionModeServerFixture<Program> serverFixture,
        ITestOutputHelper output)
        : base(browserFixture, serverFixture, output)
    {
    }

    protected override void InitializeAsyncCore()
    {
        Navigate(ServerPathBase);
        app = Browser.MountTestComponent<SampleQuickGridComponent>();
    }

    [Fact]
    public void CanColumnSortByInt()
    {
        var grid = app.FindElement(By.CssSelector("#grid > table"));
        var idColumnSortButton = grid.FindElement(By.CssSelector("thead > tr > th:nth-child(1) > div > button"));

        // Click twice to sort by descending
        idColumnSortButton.Click();
        idColumnSortButton.Click();

        var firstRow = grid.FindElement(By.CssSelector("tbody > tr:nth-child(1)"));

        //Compare first row to expected result
        Assert.Equal("12381", firstRow.FindElement(By.CssSelector("td:nth-child(1)")).Text);
        Assert.Equal("Matti", firstRow.FindElement(By.CssSelector("td:nth-child(2)")).Text);
        Assert.Equal("Karttunen", firstRow.FindElement(By.CssSelector("td:nth-child(3)")).Text);
        Assert.Equal("1981-06-04", firstRow.FindElement(By.CssSelector("td:nth-child(4)")).Text);
    }

    [Fact]
    public void CanColumnSortByString()
    {
        var grid = app.FindElement(By.CssSelector("#grid > table"));
        var firstNameColumnSortButton = grid.FindElement(By.CssSelector("thead > tr > th:nth-child(2) > div > button.col-title"));

        // Click twice to sort by descending
        firstNameColumnSortButton.Click();
        firstNameColumnSortButton.Click();

        var firstRow = grid.FindElement(By.CssSelector("tbody > tr:nth-child(1)"));

        //Compare first row to expected result
        Assert.Equal("12379", firstRow.FindElement(By.CssSelector("td:nth-child(1)")).Text);
        Assert.Equal("Zbyszek", firstRow.FindElement(By.CssSelector("td:nth-child(2)")).Text);
        Assert.Equal("Piestrzeniewicz", firstRow.FindElement(By.CssSelector("td:nth-child(3)")).Text);
        Assert.Equal("1981-04-02", firstRow.FindElement(By.CssSelector("td:nth-child(4)")).Text);
    }

    [Fact]
    public void CanColumnSortByDateOnly()
    {
        var grid = app.FindElement(By.CssSelector("#grid > table"));
        var birthDateColumnSortButton = grid.FindElement(By.CssSelector("thead > tr > th:nth-child(4) > div > button"));

        // Click twice to sort by descending
        birthDateColumnSortButton.Click();
        birthDateColumnSortButton.Click();

        var firstRow = grid.FindElement(By.CssSelector("tbody > tr:nth-child(1)"));

        //Compare first row to expected result
        Assert.Equal("12364", firstRow.FindElement(By.CssSelector("td:nth-child(1)")).Text);
        Assert.Equal("Paolo", firstRow.FindElement(By.CssSelector("td:nth-child(2)")).Text);
        Assert.Equal("Accorti", firstRow.FindElement(By.CssSelector("td:nth-child(3)")).Text);
        Assert.Equal("2018-05-18", firstRow.FindElement(By.CssSelector("td:nth-child(4)")).Text);
    }

    [Fact]
    public void PaginatorCorrectItemsPerPage()
    {
        var grid = app.FindElement(By.ClassName("quickgrid"));
        var rowCount = grid.FindElements(By.CssSelector("tbody > tr")).Count;
        Assert.Equal(10, rowCount);

        app.FindElement(By.ClassName("go-next")).Click();

        rowCount = grid.FindElements(By.CssSelector("tbody > tr")).Count;
        Assert.Equal(10, rowCount);
    }

    [Fact]
    public void PaginatorDisplaysCorrectItemCount()
    {
        var paginator = app.FindElement(By.ClassName("paginator"));

        var paginatorCount = paginator.FindElement(By.CssSelector("div > strong")).Text;
        var currentPageNumber = paginator.FindElement(By.CssSelector("nav > div > strong:nth-child(1)")).Text;
        var totalPageNumber = paginator.FindElement(By.CssSelector("nav > div > strong:nth-child(2)")).Text;

        Assert.Equal("43", paginatorCount);
        Assert.Equal("1", currentPageNumber);
        Assert.Equal("5", totalPageNumber);
    }

    [Fact]
    public void AdditionalAttributesApplied()
    {
        var grid = app.FindElement(By.CssSelector("#grid > table"));
        Assert.Equal("somevalue", grid.GetDomAttribute("custom-attrib"));
        Assert.Contains("custom-class-attrib", grid.GetDomAttribute("class")?.Split(" "));
    }

    [Fact]
    public void RowClassApplied()
    {
        var grid = app.FindElement(By.CssSelector("#grid > table"));
        var rows = grid.FindElements(By.CssSelector("tbody > tr"));

        bool isJulieRowFound = false;
        foreach (var row in rows)
        {
            var firstName = row.FindElement(By.CssSelector("td:nth-child(2)")).Text;
            if (firstName == "Julie")
            {
                isJulieRowFound = true;
                Assert.Equal("row-clickable highlight", row.GetDomAttribute("class"));
            }
            else
            {
                Assert.Equal("row-clickable", row.GetDomAttribute("class"));
            }
        }

        if (!isJulieRowFound)
        {
            Assert.Fail("No row found for Julie to highlight.");
        }
    }

    [Fact]
    public void RowStyleApplied()
    {
        var grid = app.FindElement(By.CssSelector("#grid > table"));
        var birthDateColumn = grid.FindElement(By.CssSelector("thead > tr > th:nth-child(4)"));
        var ageColumn = grid.FindElement(By.CssSelector("thead > tr > th:nth-child(5)"));

        Assert.Contains("col-justify-center", birthDateColumn.GetAttribute("class"));
        Assert.Contains("col-justify-right", ageColumn.GetAttribute("class"));
        Assert.Equal("center", Browser.ExecuteJavaScript<string>(@"
        const p = document.querySelector('tbody > tr:first-child > td:nth-child(4)');
        return p ? getComputedStyle(p).textAlign : null;"));
        Assert.Equal("right", Browser.ExecuteJavaScript<string>(@"
        const p = document.querySelector('tbody > tr:first-child > td:nth-child(5)');
        return p ? getComputedStyle(p).textAlign : null;"));
    }

    [Fact]
    public void CanOpenColumnOptions()
    {
        var grid = app.FindElement(By.CssSelector("#grid > table"));
        var firstNameColumnOptionsButton = grid.FindElement(By.CssSelector("thead > tr > th:nth-child(2) > div > button[title=\"Column options\"]"));

        firstNameColumnOptionsButton.Click();

        var firstNameSearchSelector = "#grid > table > thead > tr > th:nth-child(2) input[type=search]";
        Browser.Exists(By.CssSelector(firstNameSearchSelector));
    }

    [Fact]
    public void CanCloseColumnOptionsByBlurring()
    {
        var grid = app.FindElement(By.CssSelector("#grid > table"));
        var firstNameColumnOptionsButton = grid.FindElement(By.CssSelector("thead > tr > th:nth-child(2) > div > button[title=\"Column options\"]"));

        firstNameColumnOptionsButton.Click();

        // Click outside the column options to close
        grid.Click();

        var firstNameSearchSelector = "#grid > table > thead > tr > th:nth-child(2) input[type=search]";
        Browser.DoesNotExist(By.CssSelector(firstNameSearchSelector));
    }

    [Fact]
    public void CanCloseColumnOptionsByHideColumnOptionsAsync()
    {
        var grid = app.FindElement(By.CssSelector("#grid > table"));
        var firstNameColumnOptionsButton = grid.FindElement(By.CssSelector("thead > tr > th:nth-child(2) > div > button[title=\"Column options\"]"));

        firstNameColumnOptionsButton.Click();

        // Click the button inside the column options popup to close, which calls QuickGrid.HideColumnOptionsAsync
        grid.FindElement(By.CssSelector("#close-column-options")).Click();

        var firstNameSearchSelector = "#grid > table > thead > tr > th:nth-child(2) input[type=search]";
        Browser.DoesNotExist(By.CssSelector(firstNameSearchSelector));
    }

    [Fact]
    public void ItemsProviderCalledOnceWithVirtualize()
    {
        app = Browser.MountTestComponent<QuickGridVirtualizeComponent>();
        Browser.Equal("1", () => app.FindElement(By.Id("items-provider-call-count")).Text);
    }

    [Fact]
    public void FilterUsingSetCurrentPageDoesNotCauseExtraRefresh()
    {
        app = Browser.MountTestComponent<QuickGridFilterComponent>();

        Browser.Equal("1", () => app.FindElement(By.Id("items-provider-calls")).Text);

        var filterInput = app.FindElement(By.Id("filter-input"));
        filterInput.Clear();
        filterInput.SendKeys("Item 1");
        app.FindElement(By.Id("apply-filter-reset-pagination-btn")).Click();

        Browser.Equal("2", () => app.FindElement(By.Id("items-provider-calls")).Text);
    }

    [Fact]
    public void FilterUsingRefreshDataDoesNotCauseExtraRefresh()
    {
        app = Browser.MountTestComponent<QuickGridFilterComponent>();

        Browser.Equal("1", () => app.FindElement(By.Id("items-provider-calls")).Text);

        var filterInput = app.FindElement(By.Id("filter-input"));
        filterInput.Clear();
        filterInput.SendKeys("Item 1");
        app.FindElement(By.Id("apply-filter-refresh-data-btn")).Click();

        Browser.Equal("2", () => app.FindElement(By.Id("items-provider-calls")).Text);
    }
    
    [Fact]
    public void OnRowClickTriggersCallback()
    {
        var grid = app.FindElement(By.CssSelector("#grid > table"));

        // Verify no row has been clicked yet
        Browser.Exists(By.Id("no-click"));

        // Click on the first row (Julie Smith)
        var firstRow = grid.FindElement(By.CssSelector("tbody > tr:nth-child(1)"));
        firstRow.Click();

        // Verify the callback was triggered with correct data
        Browser.Equal("PersonId: 11203", () => app.FindElement(By.Id("clicked-person-id")).Text);
        Browser.Equal("Name: Julie Smith", () => app.FindElement(By.Id("clicked-person-name")).Text);
        Browser.Equal("Click count: 1", () => app.FindElement(By.Id("click-count")).Text);

        // Click on another row (Jose Hernandez - 3rd row)
        var thirdRow = grid.FindElement(By.CssSelector("tbody > tr:nth-child(3)"));
        thirdRow.Click();

        // Verify the callback was triggered with the new row's data
        Browser.Equal("PersonId: 11898", () => app.FindElement(By.Id("clicked-person-id")).Text);
        Browser.Equal("Name: Jose Hernandez", () => app.FindElement(By.Id("clicked-person-name")).Text);
        Browser.Equal("Click count: 2", () => app.FindElement(By.Id("click-count")).Text);
    }

    [Fact]
    public void OnRowClickAppliesCursorPointerStyle()
    {
        var grid = app.FindElement(By.CssSelector("#grid > table"));

        // Verify the row has cursor: pointer style via the row-clickable class
        var cursorStyle = Browser.ExecuteJavaScript<string>(@"
            const row = document.querySelector('#grid > table > tbody > tr:nth-child(1)');
            return row ? getComputedStyle(row).cursor : null;");
        Assert.Equal("pointer", cursorStyle);
    }
}
