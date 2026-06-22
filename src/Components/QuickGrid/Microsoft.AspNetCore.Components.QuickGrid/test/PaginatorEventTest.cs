// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.QuickGrid;
using Microsoft.AspNetCore.Components.Routing;

namespace Microsoft.AspNetCore.Components.QuickGrid.Tests;

public class PaginatorEventTest
{
    [Fact]
    public async Task OnPageChanging_InvokedBeforePageChange()
    {
        // Arrange
        var paginationState = new PaginationState { ItemsPerPage = 10 };
        var onPageChangingInvoked = false;
        var pageChangingValue = 0;

        async Task HandlePageChanging(int pageIndex)
        {
            onPageChangingInvoked = true;
            pageChangingValue = pageIndex;
            await Task.CompletedTask;
        }

        var paginator = new Paginator
        {
            State = paginationState,
            OnPageChanging = EventCallback.Factory.Create<int>(new object(), (Func<int, Task>)HandlePageChanging)
        };

        // Act
        await paginator.HandlePageChangeAsync(1);

        // Assert
        Assert.True(onPageChangingInvoked);
        Assert.Equal(2, pageChangingValue); // Page 1 (0-indexed) + 1 = 2
    }

    [Fact]
    public async Task OnPageChanged_InvokedAfterPageChange()
    {
        // Arrange
        var paginationState = new PaginationState { ItemsPerPage = 10 };
        var onPageChangedInvoked = false;
        var pageChangedValue = 0;

        async Task HandlePageChanged(int pageIndex)
        {
            onPageChangedInvoked = true;
            pageChangedValue = pageIndex;
            await Task.CompletedTask;
        }

        var paginator = new Paginator
        {
            State = paginationState,
            OnPageChanged = EventCallback.Factory.Create<int>(new object(), (Func<int, Task>)HandlePageChanged)
        };

        // Act
        await paginator.HandlePageChangeAsync(2);

        // Assert
        Assert.True(onPageChangedInvoked);
        Assert.Equal(3, pageChangedValue); // Page 2 (0-indexed) + 1 = 3
    }

    [Fact]
    public async Task OnPageChanging_CalledBeforeOnPageChanged()
    {
        // Arrange
        var paginationState = new PaginationState { ItemsPerPage = 10 };
        var callOrder = new List<string>();

        async Task HandlePageChanging(int pageIndex)
        {
            callOrder.Add("OnPageChanging");
            await Task.CompletedTask;
        }

        async Task HandlePageChanged(int pageIndex)
        {
            callOrder.Add("OnPageChanged");
            await Task.CompletedTask;
        }

        var paginator = new Paginator
        {
            State = paginationState,
            OnPageChanging = EventCallback.Factory.Create<int>(new object(), (Func<int, Task>)HandlePageChanging),
            OnPageChanged = EventCallback.Factory.Create<int>(new object(), (Func<int, Task>)HandlePageChanged)
        };

        // Act
        await paginator.HandlePageChangeAsync(0);

        // Assert
        Assert.Equal(2, callOrder.Count);
        Assert.Equal("OnPageChanging", callOrder[0]);
        Assert.Equal("OnPageChanged", callOrder[1]);
    }

    [Fact]
    public async Task Events_ReceiveCorrectPageIndex()
    {
        // Arrange
        var paginationState = new PaginationState { ItemsPerPage = 10 };
        var pageChangingIndex = -1;
        var pageChangedIndex = -1;

        async Task HandlePageChanging(int pageIndex)
        {
            pageChangingIndex = pageIndex;
            await Task.CompletedTask;
        }

        async Task HandlePageChanged(int pageIndex)
        {
            pageChangedIndex = pageIndex;
            await Task.CompletedTask;
        }

        var paginator = new Paginator
        {
            State = paginationState,
            OnPageChanging = EventCallback.Factory.Create<int>(new object(), (Func<int, Task>)HandlePageChanging),
            OnPageChanged = EventCallback.Factory.Create<int>(new object(), (Func<int, Task>)HandlePageChanged)
        };

        // Act - Navigate to page 3 (index 2)
        await paginator.HandlePageChangeAsync(2);

        // Assert - Both events should receive 3 (1-indexed)
        Assert.Equal(3, pageChangingIndex);
        Assert.Equal(3, pageChangedIndex);
    }

    [Fact]
    public async Task Events_SyncCallbacks()
    {
        // Arrange
        var paginationState = new PaginationState { ItemsPerPage = 10 };
        var changingCalled = false;
        var changedCalled = false;

        // Create callbacks that don't use async
        void HandlePageChanging(int pageIndex) => changingCalled = true;
        void HandlePageChanged(int pageIndex) => changedCalled = true;

        var paginator = new Paginator
        {
            State = paginationState,
            OnPageChanging = EventCallback.Factory.CreateInferred<int>(new object(), (Action<int>)HandlePageChanging, 0),
            OnPageChanged = EventCallback.Factory.CreateInferred<int>(new object(), (Action<int>)HandlePageChanged, 0)
        };

        // Act
        await paginator.HandlePageChangeAsync(1);

        // Assert
        Assert.True(changingCalled);
        Assert.True(changedCalled);
    }

    [Fact]
    public async Task OnPageChanging_NotInvokedWhenNotSet()
    {
        // Arrange
        var paginationState = new PaginationState { ItemsPerPage = 10 };
        var onPageChangedCalled = false;

        async Task HandlePageChanged(int pageIndex)
        {
            onPageChangedCalled = true;
            await Task.CompletedTask;
        }

        var paginator = new Paginator
        {
            State = paginationState,
            OnPageChanged = EventCallback.Factory.Create<int>(new object(), (Func<int, Task>)HandlePageChanged)
            // OnPageChanging is not set
        };

        // Act
        await paginator.HandlePageChangeAsync(0);

        // Assert - Should not throw and OnPageChanged should still be called
        Assert.True(onPageChangedCalled);
    }

    [Fact]
    public async Task OnPageChanged_NotInvokedWhenNotSet()
    {
        // Arrange
        var paginationState = new PaginationState { ItemsPerPage = 10 };
        var onPageChangingCalled = false;

        async Task HandlePageChanging(int pageIndex)
        {
            onPageChangingCalled = true;
            await Task.CompletedTask;
        }

        var paginator = new Paginator
        {
            State = paginationState,
            OnPageChanging = EventCallback.Factory.Create<int>(new object(), (Func<int, Task>)HandlePageChanging)
            // OnPageChanged is not set
        };

        // Act
        await paginator.HandlePageChangeAsync(0);

        // Assert - Should not throw and OnPageChanging should still be called
        Assert.True(onPageChangingCalled);
    }

    [Fact]
    public async Task Events_MultiplePageChanges()
    {
        // Arrange
        var paginationState = new PaginationState { ItemsPerPage = 10 };
        var changingPages = new List<int>();
        var changedPages = new List<int>();

        async Task HandlePageChanging(int pageIndex)
        {
            changingPages.Add(pageIndex);
            await Task.CompletedTask;
        }

        async Task HandlePageChanged(int pageIndex)
        {
            changedPages.Add(pageIndex);
            await Task.CompletedTask;
        }

        var paginator = new Paginator
        {
            State = paginationState,
            OnPageChanging = EventCallback.Factory.Create<int>(new object(), (Func<int, Task>)HandlePageChanging),
            OnPageChanged = EventCallback.Factory.Create<int>(new object(), (Func<int, Task>)HandlePageChanged)
        };

        // Act - Multiple page changes
        await paginator.HandlePageChangeAsync(0);
        await paginator.HandlePageChangeAsync(1);
        await paginator.HandlePageChangeAsync(2);

        // Assert
        Assert.Equal(new[] { 1, 2, 3 }, changingPages);
        Assert.Equal(new[] { 1, 2, 3 }, changedPages);
    }

    [Fact]
    public async Task Events_HandleExceptionsGracefully()
    {
        // Arrange
        var paginationState = new PaginationState { ItemsPerPage = 10 };

        async Task HandlePageChanging(int pageIndex)
        {
            throw new InvalidOperationException("Test exception");
        }

        async Task HandlePageChanged(int pageIndex)
        {
            await Task.CompletedTask;
        }

        var paginator = new Paginator
        {
            State = paginationState,
            OnPageChanging = EventCallback.Factory.Create<int>(new object(), (Func<int, Task>)HandlePageChanging),
            OnPageChanged = EventCallback.Factory.Create<int>(new object(), (Func<int, Task>)HandlePageChanged)
        };

        // Act & Assert
        // Note: The exception will be thrown since we're calling HandlePageChangeAsync directly
        // In a real Blazor app, the exception would be caught by the framework
        await Assert.ThrowsAsync<InvalidOperationException>(async () => await paginator.HandlePageChangeAsync(0));
    }

    [Fact]
    public async Task Events_FirstPageToSecondPage()
    {
        // Arrange
        var paginationState = new PaginationState { ItemsPerPage = 10 };
        var pageChangingIndex = -1;
        var pageChangedIndex = -1;

        async Task HandlePageChanging(int pageIndex)
        {
            pageChangingIndex = pageIndex;
            await Task.CompletedTask;
        }

        async Task HandlePageChanged(int pageIndex)
        {
            pageChangedIndex = pageIndex;
            await Task.CompletedTask;
        }

        var paginator = new Paginator
        {
            State = paginationState,
            OnPageChanging = EventCallback.Factory.Create<int>(new object(), (Func<int, Task>)HandlePageChanging),
            OnPageChanged = EventCallback.Factory.Create<int>(new object(), (Func<int, Task>)HandlePageChanged)
        };

        // Act - Navigate from page 1 (index 0) to page 2 (index 1)
        await paginator.HandlePageChangeAsync(1);

        // Assert
        Assert.Equal(2, pageChangingIndex);
        Assert.Equal(2, pageChangedIndex);
    }

    [Fact]
    public async Task Events_LastPageNavigation()
    {
        // Arrange
        var paginationState = new PaginationState { ItemsPerPage = 10 };
        var pageChangingIndex = -1;
        var pageChangedIndex = -1;

        async Task HandlePageChanging(int pageIndex)
        {
            pageChangingIndex = pageIndex;
            await Task.CompletedTask;
        }

        async Task HandlePageChanged(int pageIndex)
        {
            pageChangedIndex = pageIndex;
            await Task.CompletedTask;
        }

        var paginator = new Paginator
        {
            State = paginationState,
            OnPageChanging = EventCallback.Factory.Create<int>(new object(), (Func<int, Task>)HandlePageChanging),
            OnPageChanged = EventCallback.Factory.Create<int>(new object(), (Func<int, Task>)HandlePageChanged)
        };

        // Act - Navigate to last page (index 9)
        await paginator.HandlePageChangeAsync(9);

        // Assert
        Assert.Equal(10, pageChangingIndex);
        Assert.Equal(10, pageChangedIndex);
    }

    [Fact]
    public async Task HandlePageChangeAsync_AllowsNavigationToLastPage()
    {
        // Arrange
        var paginationState = new PaginationState { ItemsPerPage = 10 };
        await paginationState.SetTotalItemCountAsync(50);
        var pageChangedIndex = -1;
        async Task HandlePageChanged(int pageIndex)
        {
            pageChangedIndex = pageIndex;
            await Task.CompletedTask;
        }

        var paginator = new Paginator
        {
            State = paginationState,
            OnPageChanged = EventCallback.Factory.Create<int>(new object(), (Func<int, Task>)HandlePageChanged)
        };

        // Act
        await paginator.HandlePageChangeAsync(4);

        // Assert
        Assert.Equal(5, pageChangedIndex);
        Assert.Equal(4, paginationState.CurrentPageIndex);
    }

    [Fact]
    public async Task HandlePageChangeAsync_AllowsZeroPageIndex()
    {
        // Arrange
        var paginationState = new PaginationState { ItemsPerPage = 10 };

        var pageChangedIndex = -1;
        async Task HandlePageChanged(int pageIndex)
        {
            pageChangedIndex = pageIndex;
            await Task.CompletedTask;
        }

        var paginator = new Paginator
        {
            State = paginationState,
            OnPageChanged = EventCallback.Factory.Create<int>(new object(), (Func<int, Task>)HandlePageChanged)
        };

        // Act
        await paginator.HandlePageChangeAsync(0);

        // Assert
        Assert.Equal(1, pageChangedIndex);
        Assert.Equal(0, paginationState.CurrentPageIndex);
    }

    [Fact]
    public async Task HandlePageChangeAsync_AllowsLargePageIndex_WhenLastPageIndexUnknown()
    {
        // Arrange
        var paginationState = new PaginationState { ItemsPerPage = 10 };
        Assert.Null(paginationState.TotalItemCount);

        var pageChangedIndex = -1;
        async Task HandlePageChanged(int pageIndex)
        {
            pageChangedIndex = pageIndex;
            await Task.CompletedTask;
        }

        var paginator = new Paginator
        {
            State = paginationState,
            OnPageChanged = EventCallback.Factory.Create<int>(new object(), (Func<int, Task>)HandlePageChanged)
        };

        // Act
        await paginator.HandlePageChangeAsync(999);

        // Assert
        Assert.Equal(1000, pageChangedIndex);
        Assert.Equal(999, paginationState.CurrentPageIndex);
    }

    [Fact]
    public async Task OnPageChanging_Throws_StateIsNotUpdated_AndOnPageChangedNotInvoked()
    {
        // Arrange
        var paginationState = new PaginationState { ItemsPerPage = 10 };
        var initialIndex = paginationState.CurrentPageIndex;
        var changedInvoked = false;

        async Task HandlePageChanging(int pageIndex)
        {
            await Task.Yield();
            throw new InvalidOperationException("Test exception");
        }

        async Task HandlePageChanged(int pageIndex)
        {
            changedInvoked = true;
            await Task.CompletedTask;
        }

        var paginator = new Paginator
        {
            State = paginationState,
            OnPageChanging = EventCallback.Factory.Create<int>(new object(), (Func<int, Task>)HandlePageChanging),
            OnPageChanged = EventCallback.Factory.Create<int>(new object(), (Func<int, Task>)HandlePageChanged)
        };

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await paginator.HandlePageChangeAsync(2));

        Assert.Equal(initialIndex, paginationState.CurrentPageIndex);
        Assert.False(changedInvoked);
    }

    [Fact]
    public async Task Events_OnPageChangingObservesPreviousIndex_OnPageChangedObservesNewIndex()
    {
        // Arrange
        var paginationState = new PaginationState { ItemsPerPage = 10 };
        await paginationState.SetCurrentPageIndexAsync(1);

        int? indexObservedDuringChanging = null;
        int? indexObservedDuringChanged = null;

        async Task HandlePageChanging(int pageNumber)
        {
            indexObservedDuringChanging = paginationState.CurrentPageIndex;
            await Task.CompletedTask;
        }

        async Task HandlePageChanged(int pageNumber)
        {
            indexObservedDuringChanged = paginationState.CurrentPageIndex;
            await Task.CompletedTask;
        }

        var paginator = new Paginator
        {
            State = paginationState,
            OnPageChanging = EventCallback.Factory.Create<int>(new object(), (Func<int, Task>)HandlePageChanging),
            OnPageChanged = EventCallback.Factory.Create<int>(new object(), (Func<int, Task>)HandlePageChanged)
        };

        // Act
        await paginator.HandlePageChangeAsync(4);

        // Assert
        Assert.Equal(1, indexObservedDuringChanging);
        Assert.Equal(4, indexObservedDuringChanged);
        Assert.Equal(4, paginationState.CurrentPageIndex);
    }

    [Fact]
    public async Task Events_DelayedOnPageChanging_StateUpdatedOnlyAfterChangingCompletes()
    {
        // Arrange
        var paginationState = new PaginationState { ItemsPerPage = 10 };
        var changingTcs = new TaskCompletionSource();
        var changingStarted = new TaskCompletionSource();
        var pageIndexAtChangedTime = -1;

        async Task HandlePageChanging(int pageNumber)
        {
            changingStarted.TrySetResult();
            await changingTcs.Task;
        }

        async Task HandlePageChanged(int pageNumber)
        {
            pageIndexAtChangedTime = paginationState.CurrentPageIndex;
            await Task.CompletedTask;
        }

        var paginator = new Paginator
        {
            State = paginationState,
            OnPageChanging = EventCallback.Factory.Create<int>(new object(), (Func<int, Task>)HandlePageChanging),
            OnPageChanged = EventCallback.Factory.Create<int>(new object(), (Func<int, Task>)HandlePageChanged)
        };

        // Act
        var pageChangeTask = paginator.HandlePageChangeAsync(3);

        await changingStarted.Task;
        Assert.Equal(0, paginationState.CurrentPageIndex);

        changingTcs.SetResult();
        await pageChangeTask;

        // Assert
        Assert.Equal(3, paginationState.CurrentPageIndex);
        Assert.Equal(3, pageIndexAtChangedTime);
    }

    [Fact]
    public async Task Events_DeliverOneBasedPageNumber_AcrossKnownPages()
    {
        var paginationState = new PaginationState { ItemsPerPage = 10 };
        await paginationState.SetTotalItemCountAsync(100);

        var changingValues = new List<int>();
        var changedValues = new List<int>();

        async Task HandlePageChanging(int pageNumber)
        {
            changingValues.Add(pageNumber);
            await Task.CompletedTask;
        }

        async Task HandlePageChanged(int pageNumber)
        {
            changedValues.Add(pageNumber);
            await Task.CompletedTask;
        }

        var paginator = new Paginator
        {
            State = paginationState,
            OnPageChanging = EventCallback.Factory.Create<int>(new object(), (Func<int, Task>)HandlePageChanging),
            OnPageChanged = EventCallback.Factory.Create<int>(new object(), (Func<int, Task>)HandlePageChanged)
        };

        // Act
        for (var index = 0; index <= 9; index++)
        {
            await paginator.HandlePageChangeAsync(index);
        }

        // Assert
        Assert.Equal(new[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 }, changingValues);
        Assert.Equal(new[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 }, changedValues);
    }

    [Fact]
    public async Task HandlePageChangeAsync_DoesNotThrow_WhenExceedingLastPageIndex()
    {
        // Arrange
        var paginationState = new PaginationState { ItemsPerPage = 10 };
        await paginationState.SetTotalItemCountAsync(50);

        var changingValue = -1;
        var changedValue = -1;

        async Task HandlePageChanging(int pageNumber)
        {
            changingValue = pageNumber;
            await Task.CompletedTask;
        }

        async Task HandlePageChanged(int pageNumber)
        {
            changedValue = pageNumber;
            await Task.CompletedTask;
        }

        var paginator = new Paginator
        {
            State = paginationState,
            OnPageChanging = EventCallback.Factory.Create<int>(new object(), (Func<int, Task>)HandlePageChanging),
            OnPageChanged = EventCallback.Factory.Create<int>(new object(), (Func<int, Task>)HandlePageChanged)
        };

        // Act
        await paginator.HandlePageChangeAsync(5);

        // Assert
        Assert.Equal(6, changingValue);
        Assert.Equal(6, changedValue);
        Assert.Equal(5, paginationState.CurrentPageIndex);
    }

    [Fact]
    public async Task HandlePageChangeAsync_DoesNotThrow_OnNegativePageIndex()
    {
        // Arrange
        var paginationState = new PaginationState { ItemsPerPage = 10 };

        var changingValue = int.MinValue;
        var changedValue = int.MinValue;

        async Task HandlePageChanging(int pageNumber)
        {
            changingValue = pageNumber;
            await Task.CompletedTask;
        }

        async Task HandlePageChanged(int pageNumber)
        {
            changedValue = pageNumber;
            await Task.CompletedTask;
        }

        var paginator = new Paginator
        {
            State = paginationState,
            OnPageChanging = EventCallback.Factory.Create<int>(new object(), (Func<int, Task>)HandlePageChanging),
            OnPageChanged = EventCallback.Factory.Create<int>(new object(), (Func<int, Task>)HandlePageChanged)
        };

        // Act
        await paginator.HandlePageChangeAsync(-1);

        // Assert
        Assert.Equal(0, changingValue);
        Assert.Equal(0, changedValue);
        Assert.Equal(-1, paginationState.CurrentPageIndex);
    }
}
