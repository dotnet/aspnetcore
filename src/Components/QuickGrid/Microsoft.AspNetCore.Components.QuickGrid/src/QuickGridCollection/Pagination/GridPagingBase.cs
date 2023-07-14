// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components.QuickGrid.QuickGridCollection.Pagination;

/// <summary>
/// 
/// </summary>
public abstract class GridPagingBase : ComponentBase
{
    /// <summary>
    /// Gets or sets the pagination state.
    /// </summary>
    [Parameter, EditorRequired] public required GridPagingState PaginationState { get; set; }

    /// <summary>
    /// Gets or sets the total number of items.
    /// </summary>
    public int TotalItems { get => PaginationState.TotalItems; set => PaginationState.TotalItems = value; }

    /// <summary>
    /// Gets or sets the number of items per page.
    /// </summary>
    public int ItemsPerPage { get => PaginationState.ItemsPerPage; set => PaginationState.ItemsPerPage = value; }

    /// <summary>
    /// Gets or sets the index of the current page.
    /// </summary>
    public int CurrentPage { get => PaginationState.CurrentPage; set => PaginationState.CurrentPage = value; }

    /// <summary>
    /// Gets the total number of pages.
    /// </summary>
    public int PageCount { get => PaginationState.PageCount; }

    /// <summary>
    /// Gets a value indicating whether the current page has a previous page.
    /// </summary>
    public bool HasPreviousPage => CurrentPage > 1;

    /// <summary>
    /// Gets a value indicating whether the current page has a next page.
    /// </summary>
    public bool HasNextPage => CurrentPage < PageCount;

    /// <summary>
    /// Navigates to the previous page.
    /// </summary>
    public virtual void PreviousPage()
    {
        CurrentPage--;
    }

    /// <summary>
    /// Navigates to the next page.
    /// </summary>
    public virtual void NextPage()
    {
        CurrentPage++;
    }

    /// <summary>
    /// Navigates to a specific page.
    /// </summary>
    /// <param name="pageIndex">The index of the page to navigate to.</param>
    public virtual void GoToPage(int pageIndex)
    {
        if (pageIndex != CurrentPage + 1)
        {
            PaginationState.NextLink = null;
        }

        CurrentPage = pageIndex;
    }

    /// <summary>
    /// Distributes page numbers into an array.
    /// </summary>
    /// <returns> An array of integers representing the distributed page numbers. 0 indicates that there is no page number at that index.</returns>
    public int[] DistributePages()
    {
        var selectedPage = CurrentPage;
        var pageCount = PageCount;
        int[] result;

        if (pageCount <= 11)
        {
            result = new int[pageCount];
            for (var i = 0; i < pageCount; i++)
            {
                result[i] = i + 1;
            }
        }
        else
        {
            result = new int[11];
            if (selectedPage <= 5)
            {
                for (var i = 0; i < 8; i++)
                {
                    result[i] = i + 1;
                }
                result[9] = pageCount - 1;
                result[10] = pageCount;
            }
            else if (selectedPage >= pageCount - 4)
            {
                result[0] = 1;
                result[1] = 2;
                for (var j = 3; j < 11; j++)
                {
                    result[j] = pageCount - (10 - j);
                }
                result[9] = pageCount - 1;
                result[10] = pageCount;
            }
            else
            {
                result[0] = 1;
                result[10] = pageCount;
                for (var j = 2; j < 9; j++)
                {
                    result[j] = selectedPage + (j - 5);
                }
            }
        }
        return result;
    }

}
