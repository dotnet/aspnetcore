// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components.QuickGrid.QuickGridCollection;

/// <summary>
/// Provides data for the <see cref="GridPagingState.PageChanged"/> event.
/// </summary>
public class GridPageChangedEventArgs(GridPagingState pagingState) : EventArgs
{
    private readonly GridPagingState pagingState = pagingState;

    /// <summary>
    /// Gets or sets the link to the next page.
    /// </summary>
    public string? NextLink { get => pagingState.NextLink; set => pagingState.NextLink = value; }

    /// <summary>
    /// Gets the number of items to skip for the current page.
    /// </summary>
    public int Skip => pagingState.Skip;

    /// <summary>
    /// Gets the number of items per page.
    /// </summary>
    public int ItemsPerPage => pagingState.ItemsPerPage;

    /// <summary>
    /// Gets or sets the total number of items.
    /// </summary>
    public int TotalItems { get => pagingState.TotalItems; set => pagingState.TotalItems = value; }
}
