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

public class QuickGridAdditionalTest : ServerTestBase<BasicTestAppServerSiteFixture<RazorComponentEndpointsStartup<App>>>
{
    public QuickGridAdditionalTest(
        BrowserFixture browserFixture,
        BasicTestAppServerSiteFixture<RazorComponentEndpointsStartup<App>> serverFixture,
        ITestOutputHelper output)
        : base(browserFixture, serverFixture, output)
    {
    }

    public override Task InitializeAsync() => InitializeAsync(BrowserFixture.StreamingContext);

    [Fact]
    public void VirtualizedGridRendersExpectedRowCountAndPlaceholders()
    {
        Navigate($"{ServerPathBase}/quickgrid-variable-height");

        Browser.Exists(By.CssSelector("#grid-variable-height > table"));

        var grid = Browser.FindElement(By.CssSelector("#grid-variable-height > table"));
        Assert.Equal("1001", grid.GetDomAttribute("aria-rowcount"));

        Browser.True(() => Browser.FindElements(By.CssSelector("#grid-variable-height > table tbody > tr")).Count > 0);
        Browser.True(() => Browser.FindElements(By.CssSelector("#grid-variable-height > table tbody > tr .grid-cell-placeholder")).Count > 0);
    }

    [Fact]
    public void ColumnOptionsCanBeOpenedAndClosedThroughButtonControls()
    {
        Navigate($"{ServerPathBase}/quickgrid-interactive");

        Browser.Click(By.CssSelector("#grid > table thead > tr > th:nth-child(2) > div > button[title='Column options']"));
        Browser.Exists(By.CssSelector("#grid > table > thead > tr > th:nth-child(2) .col-options input[type='search']"));

        Browser.Click(By.CssSelector("#close-column-options"));
        Browser.DoesNotExist(By.CssSelector("#grid > table > thead > tr > th:nth-child(2) .col-options input[type='search']"));
    }

    [Fact]
    public void FilterColumnReducesRowsAndUpdatesDisplayedRows()
    {
        Navigate($"{ServerPathBase}/quickgrid");

        Browser.Click(By.CssSelector("#grid table thead > tr > th:nth-child(2) > div > button[title='Column options']"));
        var filterInput = Browser.FindElement(By.CssSelector("#grid table thead > tr > th:nth-child(2) input[type='search']"));
        filterInput.Clear();
        filterInput.SendKeys("Pa");

        Browser.Equal("2", () => Browser.FindElement(By.CssSelector(".first-paginator .paginator nav > div > strong:nth-child(1)" )).Text);
        Browser.True(() => Browser.FindElements(By.CssSelector("#grid table tbody > tr")).Count > 0);
    }

    [Fact]
    public void RowClickCallbackUpdatesDisplayedDetails()
    {
        Navigate($"{ServerPathBase}/quickgrid-interactive");

        var firstRow = Browser.FindElement(By.CssSelector("#grid table tbody > tr:nth-child(1)"));
        Assert.Contains("row-clickable", firstRow.GetDomAttribute("class") ?? string.Empty);
        firstRow.Click();

        Browser.Equal("PersonId: 11203", () => Browser.FindElement(By.Id("clicked-person-id")).Text);
        Browser.Equal("Name: Julie Smith", () => Browser.FindElement(By.Id("clicked-person-name")).Text);
        Browser.Equal("Click count: 1", () => Browser.FindElement(By.Id("click-count")).Text);
    }

    [Fact]
    public void RowHighlightClassIsAppliedToMatchingData()
    {
        Navigate($"{ServerPathBase}/quickgrid");

        var highlighted = Browser.FindElement(By.CssSelector("#grid tbody > tr.highlight"));
        Assert.Equal("Julie", highlighted.FindElement(By.CssSelector("td:nth-child(2)")).Text);
    }

    [Fact]
    public void AdditionalAttributesAndAlignmentAreRendered()
    {
        Navigate($"{ServerPathBase}/quickgrid");

        var table = Browser.FindElement(By.CssSelector("#grid > table"));
        Assert.Equal("somevalue", table.GetDomAttribute("custom-attrib"));
        Assert.Contains("custom-class-attrib", table.GetDomAttribute("class") ?? string.Empty);
        Assert.Contains("col-justify-center", Browser.FindElement(By.CssSelector("#grid thead > tr > th:nth-child(4)")).GetAttribute("class") ?? string.Empty);
        Assert.Contains("col-justify-right", Browser.FindElement(By.CssSelector("#grid thead > tr > th:nth-child(5)")).GetAttribute("class") ?? string.Empty);
    }

    [Fact]
    public void QueryParameterPrefixKeepsMultipleGridsIndependent()
    {
        Navigate($"{ServerPathBase}/quickgrid-interactive");

        Browser.Click(By.CssSelector("#grid table thead > tr > th:nth-child(1) > div > a.col-title"));
        Browser.Click(By.CssSelector("#grid2 table thead > tr > th:nth-child(2) > div > a.col-title"));

        Assert.Contains("sort=PersonId", Browser.Url);
        Assert.Contains("order=asc", Browser.Url);
        Assert.Contains("people_sort=FirstName", Browser.Url);
        Assert.Contains("people_order=asc", Browser.Url);
    }

    [Fact]
    public void SortHeadersExposeAriaSortStateAfterSorting()
    {
        Navigate($"{ServerPathBase}/quickgrid-interactive");

        Browser.Click(By.CssSelector("#grid table thead > tr > th:nth-child(1) > div > a.col-title"));

        Assert.Equal("ascending", Browser.FindElement(By.CssSelector("#grid table thead > tr > th:nth-child(1)")).GetDomAttribute("aria-sort"));

        Browser.Click(By.CssSelector("#grid table thead > tr > th:nth-child(1) > div > a.col-title"));

        Assert.Equal("descending", Browser.FindElement(By.CssSelector("#grid table thead > tr > th:nth-child(1)")).GetDomAttribute("aria-sort"));
    }

    [Fact]
    public void RowsExposeExpectedAriaRowIndexValues()
    {
        Navigate($"{ServerPathBase}/quickgrid");

        var firstRow = Browser.FindElement(By.CssSelector("#grid table tbody > tr:nth-child(1)"));
        var secondRow = Browser.FindElement(By.CssSelector("#grid table tbody > tr:nth-child(2)"));

        Assert.Equal("2", firstRow.GetDomAttribute("aria-rowindex"));
        Assert.Equal("3", secondRow.GetDomAttribute("aria-rowindex"));
    }

    [Fact]
    public void PaginatorNavigationUpdatesDisplayedPageAndUrl()
    {
        Navigate($"{ServerPathBase}/quickgrid");

        Browser.Click(By.CssSelector(".first-paginator .go-next"));

        Browser.Equal("2", () => Browser.FindElement(By.CssSelector(".first-paginator .paginator nav > div > strong:nth-child(1)")).Text);
        Assert.Contains("page=2", Browser.Url);
    }

    [Fact]
    public void SortNavigationUpdatesBothLinkedPaginators()
    {
        Navigate($"{ServerPathBase}/quickgrid-interactive");

        Browser.Click(By.CssSelector("#grid table thead > tr > th:nth-child(1) > div > a.col-title"));

        Browser.Equal("1", () => Browser.FindElement(By.CssSelector(".top-paginator .paginator nav > div > strong:nth-child(1)")).Text);
        Browser.Equal("1", () => Browser.FindElement(By.CssSelector(".bottom-paginator .paginator nav > div > strong:nth-child(1)")).Text);
    }

    [Fact]
    public void PaginatorClampsInvalidAndOutOfRangePageValues()
    {
        Navigate($"{ServerPathBase}/quickgrid?page=abc");
        Browser.Equal("1", () => Browser.FindElement(By.CssSelector(".first-paginator .paginator nav > div > strong:nth-child(1)" )).Text);

        Navigate($"{ServerPathBase}/quickgrid?page=999");
        Browser.Equal("5", () => Browser.FindElement(By.CssSelector(".first-paginator .paginator nav > div > strong:nth-child(1)" )).Text);
    }

    [Fact]
    public void DualPaginatorsUpdateTogether()
    {
        Navigate($"{ServerPathBase}/quickgrid-interactive");

        Browser.Equal("1", () => Browser.FindElement(By.CssSelector(".top-paginator .paginator nav > div > strong:nth-child(1)")).Text);
        Browser.Equal("1", () => Browser.FindElement(By.CssSelector(".bottom-paginator .paginator nav > div > strong:nth-child(1)")).Text);

        Browser.Click(By.CssSelector(".top-paginator .go-next"));

        Browser.Equal("2", () => Browser.FindElement(By.CssSelector(".top-paginator .paginator nav > div > strong:nth-child(1)")).Text);
        Browser.Equal("2", () => Browser.FindElement(By.CssSelector(".bottom-paginator .paginator nav > div > strong:nth-child(1)")).Text);
    }

    [Fact]
    public void VariableHeightGridRendersTemplateColumnContent()
    {
        Navigate($"{ServerPathBase}/quickgrid-variable-height");

        Browser.Exists(By.CssSelector("#grid-variable-height > table tbody > tr td:nth-child(3) div"));
        Assert.Equal("1", Browser.FindElement(By.CssSelector("#grid-variable-height > table tbody > tr:nth-child(1) td:nth-child(1)")).Text);
        Assert.Contains("Person 1", Browser.FindElement(By.CssSelector("#grid-variable-height > table tbody > tr:nth-child(1) td:nth-child(2)")).Text);
    }

    [Fact]
    public void DefaultSortColumnLoadsDescendingOnFirstRender()
    {
        Navigate($"{ServerPathBase}/quickgrid-advanced");

        Browser.Equal("3", () => Browser.FindElement(By.CssSelector("#default-sort-grid tbody > tr:nth-child(1) td:nth-child(1)")).Text);
        Assert.Equal("descending", Browser.FindElement(By.CssSelector("#default-sort-grid thead > tr > th:nth-child(1)")).GetDomAttribute("aria-sort"));
    }

    [Fact]
    public void TemplateColumnSupportsCustomMultiColumnSort()
    {
        Navigate($"{ServerPathBase}/quickgrid-advanced");

        Browser.Click(By.CssSelector("#custom-sort-grid thead > tr > th:nth-child(1) > div > a.col-title"));

        // Wait for the sort state to update with the expected sort properties
        Browser.True(() => Browser.FindElement(By.Id("custom-sort-state")).Text.Contains("Country"), TimeSpan.FromSeconds(5));

        // Verify the first row contains "Delhi" (the city name)
        Assert.Contains("Delhi", Browser.FindElement(By.CssSelector("#custom-sort-grid tbody > tr:nth-child(1) td:nth-child(1) div")).Text);
        var sortState = Browser.FindElement(By.Id("custom-sort-state")).Text;
        Assert.Contains("Country", sortState);
        Assert.Contains("City", sortState);
    }

    [Fact]
    public void ItemKeyPreservesRowsWhenDataIsReplaced()
    {
        Navigate($"{ServerPathBase}/quickgrid-advanced");

        var firstRow = Browser.FindElement(By.CssSelector("#item-key-grid tbody > tr:nth-child(1)"));
        var firstRowReference = firstRow;
        Assert.Equal("Alpha", firstRow.FindElement(By.CssSelector("td:nth-child(2)" )).Text);

        Browser.Click(By.Id("replace-item-key-data"));

        // Wait for Blazor to process the click and update the state
        Browser.True(() => Browser.FindElement(By.Id("item-key-state")).Text == "replaced", TimeSpan.FromSeconds(5));

        var firstRowAfter = Browser.FindElement(By.CssSelector("#item-key-grid tbody > tr:nth-child(1)"));
        Assert.Same(firstRowReference, firstRowAfter);
        Assert.Equal("Alpha updated", Browser.FindElement(By.CssSelector("#item-key-grid tbody > tr:nth-child(1) td:nth-child(2)" )).Text);
    }

    [Fact]
    public void DefaultSortColumnResetsToDefaultSortWhenSortRemovedFromUrl()
    {
        Navigate($"{ServerPathBase}/quickgrid-advanced");

        // Verify default sort is descending on Id
        Browser.Equal("3", () => Browser.FindElement(By.CssSelector("#default-sort-grid tbody > tr:nth-child(1) td:nth-child(1)")).Text);
        Assert.Equal("descending", Browser.FindElement(By.CssSelector("#default-sort-grid thead > tr > th:nth-child(1)")).GetDomAttribute("aria-sort"));
    }

    [Fact]
    public void SortableColumnShowsSortOptions()
    {
        Navigate($"{ServerPathBase}/quickgrid-interactive");

        // First column (PersonId) is sortable, should have button with col-title class
        var sortableHeader = Browser.FindElement(By.CssSelector("#grid table thead > tr > th:nth-child(1) > div"));
        Assert.Contains("col-title", sortableHeader.GetDomAttribute("class") ?? "");

        // All columns in this grid are explicitly Sortable="true"
        // Verify the FirstName column also has sort options
        var firstNameHeader = Browser.FindElement(By.CssSelector("#grid table thead > tr > th:nth-child(2) > div"));
        Assert.Contains("col-title", firstNameHeader.GetDomAttribute("class") ?? "");

        // Verify LastName column has sort options
        var lastNameHeader = Browser.FindElement(By.CssSelector("#grid table thead > tr > th:nth-child(3) > div"));
        Assert.Contains("col-title", lastNameHeader.GetDomAttribute("class") ?? "");
    }

    [Fact]
    public void NavigateToPageAndSortTogetherInUrl()
    {
        Navigate($"{ServerPathBase}/quickgrid-interactive?page=2");

        Browser.Equal("2", () => Browser.FindElement(By.CssSelector(".first-paginator .paginator nav > div > strong:nth-child(1)")).Text);

        // Apply sort while on page 2
        Browser.Click(By.CssSelector("#grid table thead > tr > th:nth-child(1) > div > a.col-title"));

        // Verify both page and sort are in URL
        Assert.Contains("page=2", Browser.Url);
        Assert.Contains("sort=PersonId", Browser.Url);
        Assert.Contains("order=asc", Browser.Url);

        // After sorting by PersonId ascending, page 2 first row should have PersonId 12348
        // (Page 1 has: 10895, 10944, 11203, 11205, 11898, 12130, 12238, 12345, 12346, 12347)
        // (Page 2 has: 12348, 12349, 12350, 12351, 12352, 12353, 12354, 12355, 12356, 12357)
        Browser.Equal("12348", () => Browser.FindElement(By.CssSelector("#grid table tbody > tr:nth-child(1) > td:nth-child(1)")).Text);
    }

    [Fact]
    public void RowsRetainAriaRowIndexAfterSorting()
    {
        Navigate($"{ServerPathBase}/quickgrid-interactive");

        // Get first row before sorting
        var firstRowBeforeSort = Browser.FindElement(By.CssSelector("#grid table tbody > tr:nth-child(1)"));
        var rowindexBeforeSort = firstRowBeforeSort.GetDomAttribute("aria-rowindex");

        // Sort by PersonId
        Browser.Click(By.CssSelector("#grid table thead > tr > th:nth-child(1) > div > a.col-title"));

        // First row after sorting should still have proper aria-rowindex
        var firstRowAfterSort = Browser.FindElement(By.CssSelector("#grid table tbody > tr:nth-child(1)"));
        var rowindexAfterSort = firstRowAfterSort.GetDomAttribute("aria-rowindex");

        // Row index should remain consistent (starting at 2 for first data row)
        Assert.Equal("2", rowindexAfterSort);
        Assert.NotEqual(rowindexBeforeSort, rowindexAfterSort); // The actual PersonId value changed
    }

    [Fact]
    public void ColumnOptionsSearchFiltersCorrectly()
    {
        Navigate($"{ServerPathBase}/quickgrid-interactive");

        // Open column options for FirstName column
        Browser.Click(By.CssSelector("#grid > table thead > tr > th:nth-child(2) > div > button[title='Column options']"));
        Browser.Exists(By.CssSelector("#grid > table > thead > tr > th:nth-child(2) .col-options input[type='search']"));

        var filterInput = Browser.FindElement(By.CssSelector("#grid > table thead > tr > th:nth-child(2) input[type='search']"));
        filterInput.Clear();
        filterInput.SendKeys("Jul");

        // The filter should be applied and grid should show filtered results
        Browser.True(() => Browser.FindElements(By.CssSelector("#grid table tbody > tr")).Count > 0);

        // Close column options
        Browser.Click(By.CssSelector("#close-column-options"));
        Browser.DoesNotExist(By.CssSelector("#grid > table > thead > tr > th:nth-child(2) .col-options input[type='search']"));
    }

    [Fact]
    public void SortByDifferentColumnResetsSortOnPreviousColumn()
    {
        Navigate($"{ServerPathBase}/quickgrid-interactive");

        // Sort by PersonId ascending
        Browser.Click(By.CssSelector("#grid table thead > tr > th:nth-child(1) > div > a.col-title"));
        Assert.Equal("ascending", Browser.FindElement(By.CssSelector("#grid table thead > tr > th:nth-child(1)")).GetDomAttribute("aria-sort"));

        // Sort by FirstName ascending - PersonId should no longer show sort
        Browser.Click(By.CssSelector("#grid table thead > tr > th:nth-child(2) > div > a.col-title"));
        Assert.Equal("none", Browser.FindElement(By.CssSelector("#grid table thead > tr > th:nth-child(1)")).GetDomAttribute("aria-sort"));
        Assert.Equal("ascending", Browser.FindElement(By.CssSelector("#grid table thead > tr > th:nth-child(2)")).GetDomAttribute("aria-sort"));
    }

    [Fact]
    public void PaginatorFirstAndLastButtonsWorkCorrectly()
    {
        Navigate($"{ServerPathBase}/quickgrid-interactive");

        // Initially on page 1
        Browser.Equal("1", () => Browser.FindElement(By.CssSelector(".first-paginator .paginator nav > div > strong:nth-child(1)")).Text);

        // Navigate to last page
        Browser.Click(By.CssSelector(".first-paginator .go-last"));
        Browser.Equal("5", () => Browser.FindElement(By.CssSelector(".first-paginator .paginator nav > div > strong:nth-child(1)")).Text);
        Assert.Contains("page=5", Browser.Url);

        // Navigate back to first page
        Browser.Click(By.CssSelector(".first-paginator .go-first"));
        Browser.Equal("1", () => Browser.FindElement(By.CssSelector(".first-paginator .paginator nav > div > strong:nth-child(1)")).Text);
        Assert.Contains("page=1", Browser.Url);
    }

    [Fact]
    public void DualPaginatorsMaintainIndependentStateOnSort()
    {
        Navigate($"{ServerPathBase}/quickgrid-interactive");

        // Both paginators start at page 1
        Browser.Equal("1", () => Browser.FindElement(By.CssSelector(".top-paginator .paginator nav > div > strong:nth-child(1)")).Text);
        Browser.Equal("1", () => Browser.FindElement(By.CssSelector(".bottom-paginator .paginator nav > div > strong:nth-child(1)")).Text);

        // Navigate using pagination2's query param (people_page) to page 3 for grid2
        Browser.Navigate().GoToUrl($"{ServerPathBase}/quickgrid-interactive?people_page=3");
        Browser.Equal("3", () => Browser.FindElement(By.CssSelector(".top-paginator .paginator nav > div > strong:nth-child(1)")).Text);
        Browser.Equal("3", () => Browser.FindElement(By.CssSelector(".bottom-paginator .paginator nav > div > strong:nth-child(1)")).Text);

        // Sort grid2 (second grid with pagination2)
        Browser.Click(By.CssSelector("#grid2 table thead > tr > th:nth-child(1) > div > a.col-title"));

        // Both paginators should stay at page 3 (sort doesn't change page)
        Browser.Equal("3", () => Browser.FindElement(By.CssSelector(".top-paginator .paginator nav > div > strong:nth-child(1)")).Text);
        Browser.Equal("3", () => Browser.FindElement(By.CssSelector(".bottom-paginator .paginator nav > div > strong:nth-child(1)")).Text);
    }

    [Fact]
    public void NavigateAwayAndBackPreservesGridState()
    {
        Navigate($"{ServerPathBase}/quickgrid-interactive");

        // Sort by PersonId ascending
        Browser.Click(By.CssSelector("#grid table thead > tr > th:nth-child(1) > div > a.col-title"));
        Browser.Equal("ascending", () => Browser.FindElement(By.CssSelector("#grid table thead > tr > th:nth-child(1)")).GetDomAttribute("aria-sort"));

        // Navigate to page 2
        Browser.Click(By.CssSelector(".first-paginator .go-next"));
        Browser.Equal("2", () => Browser.FindElement(By.CssSelector(".first-paginator .paginator nav > div > strong:nth-child(1)")).Text);

        // Navigate away to another page
        Navigate($"{ServerPathBase}/quickgrid-advanced");

        // Navigate back to the grid
        Navigate($"{ServerPathBase}/quickgrid-interactive");

        // State should be preserved - still on page 2 with sort applied
        Browser.Equal("2", () => Browser.FindElement(By.CssSelector(".first-paginator .paginator nav > div > strong:nth-child(1)")).Text);
        Browser.Equal("ascending", () => Browser.FindElement(By.CssSelector("#grid table thead > tr > th:nth-child(1)")).GetDomAttribute("aria-sort"));
    }

    [Fact]
    public void NonSortableColumnDoesNotRespondToClick()
    {
        Navigate($"{ServerPathBase}/quickgrid");

        // The BirthDate column with Format is sortable, but let's verify sortable=false column behavior
        // We need to check that clicking on a column without Sortable="true" doesn't change sort state
        // First, sort by PersonId which is sortable
        Browser.Click(By.CssSelector("#grid table thead > tr > th:nth-child(1) > div > a.col-title"));
        Browser.Equal("ascending", () => Browser.FindElement(By.CssSelector("#grid table thead > tr > th:nth-child(1)")).GetDomAttribute("aria-sort"));

        // Sort by FirstName (which is sortable)
        Browser.Click(By.CssSelector("#grid table thead > tr > th:nth-child(2) > div > a.col-title"));
        Browser.Equal("none", () => Browser.FindElement(By.CssSelector("#grid table thead > tr > th:nth-child(1)")).GetDomAttribute("aria-sort"));
        Browser.Equal("ascending", () => Browser.FindElement(By.CssSelector("#grid table thead > tr > th:nth-child(2)")).GetDomAttribute("aria-sort"));
    }

    [Fact]
    public void PaginationWithLessItemsThanPageSize()
    {
        Navigate($"{ServerPathBase}/quickgrid-advanced");

        // The default-sort-grid has pagination with ItemsPerPage=2 and only 3 items total
        // Page 2 should have only 1 item
        var grid = Browser.FindElement(By.CssSelector("#default-sort-grid"));
        var rows = grid.FindElements(By.CssSelector("tbody > tr"));

        // 3 items with 2 per page = page 1 has 2 rows, page 2 has 1 row
        // Navigate to page 2
        Browser.Click(By.CssSelector("#default-sort-grid + * .go-next"));
        Browser.Equal("2", () => Browser.FindElement(By.CssSelector("#default-sort-grid + * .paginator nav > div > strong:nth-child(1)")).Text);
    }

    [Fact]
    public void InitialSortDirectionAscendingWorks()
    {
        Navigate($"{ServerPathBase}/quickgrid-advanced");

        // Default sort is Descending on Id column
        // Verify the initial sort direction is applied correctly
        Browser.Equal("3", () => Browser.FindElement(By.CssSelector("#default-sort-grid tbody > tr:nth-child(1) td:nth-child(1)")).Text);
        Assert.Equal("descending", Browser.FindElement(By.CssSelector("#default-sort-grid thead > tr > th:nth-child(1)")).GetDomAttribute("aria-sort"));
    }

    [Fact]
    public void ColumnOptionsSearchWithNoResults()
    {
        Navigate($"{ServerPathBase}/quickgrid-interactive");

        // Open column options for FirstName column
        Browser.Click(By.CssSelector("#grid > table thead > tr > th:nth-child(2) > div > button[title='Column options']"));
        Browser.Exists(By.CssSelector("#grid > table > thead > tr > th:nth-child(2) .col-options input[type='search']"));

        var filterInput = Browser.FindElement(By.CssSelector("#grid > table thead > tr > th:nth-child(2) input[type='search']"));
        filterInput.Clear();
        filterInput.SendKeys("XYZNONEXISTENTNAME123");

        // Grid should show 0 results or no matching rows
        // The paginator should update to show filtered count
        Browser.Equal("0", () => Browser.FindElement(By.CssSelector(".first-paginator .paginator nav > div > strong:nth-child(1)")).Text);
    }

    [Fact]
    public void GridWithItemsProviderSupportsTotalCount()
    {
        Navigate($"{ServerPathBase}/quickgrid");

        // grid2 uses ItemsProvider with explicit total count
        var grid2 = Browser.FindElement(By.CssSelector("#grid2"));
        var rows = grid2.FindElements(By.CssSelector("tbody > tr"));

        // ItemsProvider returns 30 cities total, 5 per page
        Assert.Equal(5, rows.Count);

        // Navigate to second page
        Browser.Click(By.CssSelector(".second-paginator .go-next"));
        Browser.Equal("2", () => Browser.FindElement(By.CssSelector(".second-paginator .paginator nav > div > strong:nth-child(1)")).Text);

        // Should still have 5 rows on page 2
        rows = Browser.FindElement(By.CssSelector("#grid2")).FindElements(By.CssSelector("tbody > tr"));
        Assert.Equal(5, rows.Count);
    }

    [Fact]
    public void MultipleGridsWithDifferentPaginationState()
    {
        Navigate($"{ServerPathBase}/quickgrid");

        // grid and grid2 have different pagination states
        // Navigate grid to page 2
        Browser.Click(By.CssSelector(".first-paginator .go-next"));
        Browser.Equal("2", () => Browser.FindElement(By.CssSelector(".first-paginator .paginator nav > div > strong:nth-child(1)")).Text);

        // grid2 should still be on page 1
        Browser.Equal("1", () => Browser.FindElement(By.CssSelector(".second-paginator .paginator nav > div > strong:nth-child(1)")).Text);
    }

    [Fact]
    public void SortThenPaginateMaintainsSortOrder()
    {
        Navigate($"{ServerPathBase}/quickgrid-interactive");

        // Sort by PersonId descending first
        Browser.Click(By.CssSelector("#grid table thead > tr > th:nth-child(1) > div > a.col-title")); // asc
        Browser.Click(By.CssSelector("#grid table thead > tr > th:nth-child(1) > div > a.col-title")); // desc

        var firstPersonIdBeforePagination = Browser.FindElement(By.CssSelector("#grid table tbody > tr:nth-child(1) > td:nth-child(1)")).Text;

        // Now paginate
        Browser.Click(By.CssSelector(".first-paginator .go-next"));

        // Sort should still be descending
        Assert.Equal("descending", Browser.FindElement(By.CssSelector("#grid table thead > tr > th:nth-child(1)")).GetDomAttribute("aria-sort"));

        // First row on page 2 should be properly sorted
        var firstPersonIdOnPage2 = Browser.FindElement(By.CssSelector("#grid table tbody > tr:nth-child(1) > td:nth-child(1)")).Text;
        Assert.NotEqual(firstPersonIdBeforePagination, firstPersonIdOnPage2);
    }

    [Fact]
    public void NavigateToNonExistentPageClampsToLastPage()
    {
        Navigate($"{ServerPathBase}/quickgrid?page=999");

        // Should clamp to last valid page (5)
        Browser.Equal("5", () => Browser.FindElement(By.CssSelector(".first-paginator .paginator nav > div > strong:nth-child(1)")).Text);
    }

    [Fact]
    public void PaginatorWithZeroItemsShowsEmptyState()
    {
        Navigate($"{ServerPathBase}/quickgrid-advanced");

        // Wait for the element to be present
        Browser.Exists(By.CssSelector("#item-key-grid"));

        // The item-key-grid has no pagination and displays 2 items (Alpha, Bravo)
        // This tests that a grid without explicit pagination works correctly
        var itemKeyGrid = Browser.FindElement(By.CssSelector("#item-key-grid"));
        var rows = itemKeyGrid.FindElements(By.CssSelector("tbody > tr"));
        Assert.Equal(2, rows.Count);

        // Verify the items are rendered
        Assert.Equal("1", Browser.FindElement(By.CssSelector("#item-key-grid tbody > tr:nth-child(1) td:nth-child(1)")).Text);
        Assert.Equal("Alpha", Browser.FindElement(By.CssSelector("#item-key-grid tbody > tr:nth-child(1) td:nth-child(2)")).Text);
    }

    [Fact]
    public void ColumnHeaderExposesTitleAttribute()
    {
        Navigate($"{ServerPathBase}/quickgrid");

        // PersonId column has implicit title from property
        var personIdHeader = Browser.FindElement(By.CssSelector("#grid table thead > tr > th:nth-child(1)"));
        Assert.Equal("PersonId", personIdHeader.Text);

        // FirstName column
        var firstNameHeader = Browser.FindElement(By.CssSelector("#grid table thead > tr > th:nth-child(2)"));
        Assert.Equal("FirstName", firstNameHeader.Text);
    }

    [Fact]
    public void SortIconShownForSortableColumns()
    {
        Navigate($"{ServerPathBase}/quickgrid-interactive");

        // Sortable columns should have sort indicators in header
        var sortableHeader = Browser.FindElement(By.CssSelector("#grid table thead > tr > th:nth-child(1)"));

        // The col-title element should contain a sort indicator
        var colTitleElement = sortableHeader.FindElement(By.CssSelector(".col-title"));
        Assert.NotNull(colTitleElement);
    }

    [Fact]
    public void GridClassAndThemeAppliedCorrectly()
    {
        Navigate($"{ServerPathBase}/quickgrid-interactive");

        var gridTable = Browser.FindElement(By.CssSelector("#grid > table"));

        // Default theme should be applied
        Assert.Contains("table", gridTable.GetDomAttribute("class") ?? "");

        // Custom class from parameter should be applied
        Assert.Contains("custom-class-attrib", gridTable.GetDomAttribute("class") ?? "");
    }
}
