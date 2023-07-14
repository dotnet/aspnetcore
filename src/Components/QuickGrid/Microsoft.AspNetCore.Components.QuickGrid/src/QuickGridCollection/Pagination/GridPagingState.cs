// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components.QuickGrid.QuickGridCollection;

/// <summary>
/// Represents the pagination state of a grid.
/// </summary>
public class GridPagingState(int itemsPerPage = 20)
{
    private int pageIndex = 1;
    private int itemsPerPage = itemsPerPage;

    /// <summary>
    /// Gets or sets the total number of items.
    /// </summary>
    public int TotalItems { get; set; }

    /// <summary>
    /// Gets or sets the number of items per page.
    /// </summary>
    public int ItemsPerPage { get => itemsPerPage; set => itemsPerPage = value; }

    /// <summary>
    /// Gets or sets the index of the current page.
    /// </summary>
    public int CurrentPage { get => pageIndex; set { pageIndex = value; OnPageChanged(); } }

    /// <summary>
    /// Gets the number of items to skip for the current page.
    /// </summary>
    public int Skip => (CurrentPage - 1) * ItemsPerPage;

    /// <summary>
    /// Gets or sets the link to the next page.
    /// </summary> 
    public string? NextLink { get; set; }

    /// <summary>
    /// Gets the total number of pages.
    /// </summary>
    public int PageCount { get => (int)Math.Ceiling(TotalItems / (decimal)ItemsPerPage); }

    /// <summary>
    /// Occurs when the index of the current page changes.
    /// </summary>
    public event EventHandler<GridPageChangedEventArgs> PageChanged = default!;

    /// <summary>
    /// Raises the <see cref="PageChanged"/> event.
    /// </summary>
    private void OnPageChanged()
    {
        PageChanged.Invoke(this, new(this));
    }

}
