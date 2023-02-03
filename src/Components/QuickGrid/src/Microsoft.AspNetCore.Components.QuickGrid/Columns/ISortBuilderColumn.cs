// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components.QuickGrid;

/// <summary>
/// An interface that, if implemented by a <see cref="ColumnBase{TGridItem}"/> subclass, allows a <see cref="QuickGrid{TGridItem}"/>
/// to understand the sorting rules associated with that column.
///
/// If a <see cref="ColumnBase{TGridItem}"/> subclass does not implement this, that column can still be marked as sortable and can
/// be the current sort column, but its sorting logic cannot be applied to the data queries automatically. The developer would be
/// responsible for implementing that sorting logic separately inside their <see cref="GridItemsProvider{TGridItem}"/>.
/// </summary>
/// <typeparam name="TGridItem">The type of data represented by each row in the grid.</typeparam>
public interface ISortBuilderColumn<TGridItem>
{
    /// <summary>
    /// Gets the sorting rules associated with the column.
    /// </summary>
    public GridSort<TGridItem>? SortBuilder { get; }
}
