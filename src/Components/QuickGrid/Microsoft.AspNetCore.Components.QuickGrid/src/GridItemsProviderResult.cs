// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components.QuickGrid;

/// <summary>
/// Holds data being supplied to a <see cref="QuickGrid{TGridItem}"/>'s <see cref="QuickGrid{TGridItem}.ItemsProvider"/>.
/// </summary>
/// <typeparam name="TGridItem">The type of data represented by each row in the grid.</typeparam>
public readonly struct GridItemsProviderResult<TGridItem>
{
    /// <summary>
    /// The items being supplied.
    /// </summary>
    public required ICollection<TGridItem> Items { get; init; }

    /// <summary>
    /// The total number of items that may be displayed in the grid. This normally means the total number of items in the
    /// underlying data source after applying any filtering that is in effect.
    ///
    /// If the grid is paginated, this should include all pages. If the grid is virtualized, this should include the entire scroll range.
    /// </summary>
    public int TotalItemCount { get; init; }
}

/// <summary>
/// Provides convenience methods for constructing <see cref="GridItemsProviderResult{TGridItem}"/> instances.
/// </summary>
public static class GridItemsProviderResult
{
    // This is just to provide generic type inference, so you don't have to specify TGridItem yet again.

    /// <summary>
    /// Supplies an instance of <see cref="GridItemsProviderResult{TGridItem}"/>.
    /// </summary>
    /// <typeparam name="TGridItem">The type of data represented by each row in the grid.</typeparam>
    /// <param name="items">The items being supplied.</param>
    /// <param name="totalItemCount">The total numer of items that exist. See <see cref="GridItemsProviderResult{TGridItem}.TotalItemCount"/> for details.</param>
    /// <returns>An instance of <see cref="GridItemsProviderResult{TGridItem}"/>.</returns>
    public static GridItemsProviderResult<TGridItem> From<TGridItem>(ICollection<TGridItem> items, int totalItemCount)
        => new() { Items = items, TotalItemCount = totalItemCount };
}
