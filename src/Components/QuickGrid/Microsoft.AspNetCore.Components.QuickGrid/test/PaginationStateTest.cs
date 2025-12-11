// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading.Tasks;
using Xunit;

namespace Microsoft.AspNetCore.Components.QuickGrid.Tests;

public class PaginationTests
{
    [Fact]
    public async Task PaginationState_RaisesTotalItemCountChanged_AfterPageResetWithSameTotalCount()
    {
        // Verifies that TotalItemCountChanged fires when the page is reset due to reduced item count,
        // even when the second call to SetTotalItemCountAsync has the same total as the first call.

        // Arrange
        var pagination = new PaginationState { ItemsPerPage = 10 };
        var totalItemCountChangedCallCount = 0;
        int? lastTotalItemCount = null;

        pagination.TotalItemCountChanged += (sender, count) =>
        {
            totalItemCountChangedCallCount++;
            lastTotalItemCount = count;
        };

        // Simulate initial state: user is on page 5 (index 4) with 50 items total
        await pagination.SetCurrentPageIndexAsync(4);
        await pagination.SetTotalItemCountAsync(50);

        Assert.Equal(4, pagination.CurrentPageIndex);
        Assert.Equal(50, pagination.TotalItemCount);
        Assert.Equal(1, totalItemCountChangedCallCount);
        Assert.Equal(50, lastTotalItemCount);

        totalItemCountChangedCallCount = 0;

        // Act - Simulate query returning fewer items, triggering page reset and subsequent re-query
        // The first call sets the internal flag (fix logic)
        await pagination.SetTotalItemCountAsync(20);
        // The second call (simulating re-render) consumes the flag and raises the event
        await pagination.SetTotalItemCountAsync(20);

        // Assert
        Assert.True(totalItemCountChangedCallCount > 0,
            $"TotalItemCountChanged should have fired after pagination state stabilized, but it fired {totalItemCountChangedCallCount} times. " +
            "This means paginators won't re-render to show the updated page count.");
        Assert.Equal(20, lastTotalItemCount);
        Assert.Equal(1, pagination.CurrentPageIndex);
    }

    [Fact]
    public async Task PaginationState_ResetsToLastPage_WhenCurrentPageExceedsLastPage()
    {
        // Arrange
        var pagination = new PaginationState { ItemsPerPage = 10 };
        await pagination.SetCurrentPageIndexAsync(4);
        await pagination.SetTotalItemCountAsync(50);

        Assert.Equal(4, pagination.CurrentPageIndex);
        Assert.Equal(50, pagination.TotalItemCount);

        // Act
        await pagination.SetTotalItemCountAsync(20);

        // Assert
        Assert.Equal(1, pagination.CurrentPageIndex);
        Assert.Equal(20, pagination.TotalItemCount);
    }

    [Fact]
    public async Task PaginationState_CalculatesLastPageIndexCorrectly()
    {
        // Arrange
        var pagination = new PaginationState { ItemsPerPage = 10 };

        // Act & Assert
        await pagination.SetTotalItemCountAsync(1);
        Assert.Equal(0, pagination.LastPageIndex);

        await pagination.SetTotalItemCountAsync(10);
        Assert.Equal(0, pagination.LastPageIndex);

        await pagination.SetTotalItemCountAsync(11);
        Assert.Equal(1, pagination.LastPageIndex);

        await pagination.SetTotalItemCountAsync(20);
        Assert.Equal(1, pagination.LastPageIndex);

        await pagination.SetTotalItemCountAsync(21);
        Assert.Equal(2, pagination.LastPageIndex);
    }

    [Fact]
    public async Task PaginationState_RaisesTotalItemCountChanged_WhenTotalCountChanges()
    {
        // Arrange
        var pagination = new PaginationState { ItemsPerPage = 10 };
        var eventCallCount = 0;
        int? lastEventValue = null;

        pagination.TotalItemCountChanged += (sender, count) =>
        {
            eventCallCount++;
            lastEventValue = count;
        };

        // Act & Assert
        await pagination.SetTotalItemCountAsync(50);
        Assert.Equal(1, eventCallCount);
        Assert.Equal(50, lastEventValue);

        await pagination.SetTotalItemCountAsync(30);
        Assert.Equal(2, eventCallCount);
        Assert.Equal(30, lastEventValue);

        await pagination.SetTotalItemCountAsync(30);
        Assert.Equal(2, eventCallCount);
        Assert.Equal(30, lastEventValue);
    }

    [Fact]
    public async Task PaginationState_DoesNotResetPage_WhenCurrentPageIsStillValid()
    {
        // Arrange
        var pagination = new PaginationState { ItemsPerPage = 10 };
        var totalItemCountChangedCallCount = 0;

        pagination.TotalItemCountChanged += (sender, count) =>
        {
            totalItemCountChangedCallCount++;
        };

        await pagination.SetCurrentPageIndexAsync(2);
        await pagination.SetTotalItemCountAsync(50);

        totalItemCountChangedCallCount = 0;

        // Act
        await pagination.SetTotalItemCountAsync(40);

        // Assert
        Assert.Equal(2, pagination.CurrentPageIndex);
        Assert.Equal(1, totalItemCountChangedCallCount);
    }
}
