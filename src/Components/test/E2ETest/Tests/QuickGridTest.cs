// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using BasicTestApp;
using BasicTestApp.QuickGridTest;
using Microsoft.AspNetCore.Components.E2ETest;
using Microsoft.AspNetCore.Components.E2ETest.Infrastructure;
using Microsoft.AspNetCore.Components.E2ETest.Infrastructure.ServerFixtures;
using Microsoft.AspNetCore.E2ETesting;
using OpenQA.Selenium;
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
                Assert.Equal("highlight", row.GetDomAttribute("class"));
            }
            else
            {
                Assert.Null(row.GetDomAttribute("class"));
            }
        }

        if (!isJulieRowFound)
        {
            Assert.Fail("No row found for Julie to highlight.");
        }
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
    public void CanCloseColumnOptionsByCloseColumnOptionsAsync()
    {
        var grid = app.FindElement(By.CssSelector("#grid > table"));
        var firstNameColumnOptionsButton = grid.FindElement(By.CssSelector("thead > tr > th:nth-child(2) > div > button[title=\"Column options\"]"));

        firstNameColumnOptionsButton.Click();

        // Click the button inside the column options popup to close, which calls QuickGrid.CloseColumnOptionsAsync
        grid.FindElement(By.CssSelector("#close-column-options")).Click();

        var firstNameSearchSelector = "#grid > table > thead > tr > th:nth-child(2) input[type=search]";
        Browser.DoesNotExist(By.CssSelector(firstNameSearchSelector));
    }
}
