// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq.Expressions;

namespace Microsoft.AspNetCore.Components.QuickGrid.QuickGridCollection.Columns;

/// <summary>
/// Provides a context for custom column options
/// defined in the <see cref="ColumnCBase{TGridItem}.ColumnOptions"/> property.
/// </summary>
/// <typeparam name="TGridItem">The type of data items displayed in the grid.</typeparam>
public class ColumnOptionsContext<TGridItem>(ColumnCBase<TGridItem> column)
{
    private readonly ColumnCBase<TGridItem> column = column;

    /// <summary>
    /// Indicates whether the column options are applied.
    /// </summary>
    public bool OptionApplied { get => column.OptionApplied; set => column.OptionApplied = value; }

    /// <summary>
    /// Indicates whether column options are shown or hidden.
    /// </summary>
    public bool IsOptionVisible { get => column.IsOptionVisible; set => column.IsOptionVisible = value; }

    /// <summary>
    /// Adds or updates a filter for a specified column.
    /// If the filter expression is null, the existing filter for this column is removed.
    /// Otherwise, the existing filter for this column is updated or added to the list of filters.
    /// </summary>
    public void ApplyColumnFilter(Expression<Func<TGridItem, bool>>? expression)
    {
        column.Grid.ApplyColumnFilter(expression, column);
    }
}
