// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq.Expressions;

namespace Microsoft.AspNetCore.Components.QuickGrid.QuickGridCollection.Columns;

/// <summary>
/// Provides a context for the custom column header
/// defined in the <see cref="ColumnCBase{TGridItem}.HeaderTemplate"/> property.
/// </summary>
/// <typeparam name="TGridItem">The type of data items displayed in the grid.</typeparam>
public class HeaderTemplateContext<TGridItem>(ColumnCBase<TGridItem> column)
{

    /// <summary>
    /// Indicates whether the column is sortable.
    /// </summary>
    /// /// <remarks>
    /// if <see cref="IsSortable"/> is set to <c>true</c> you must use the method <see cref="SetPropertyExpressionAndType{TPro}(Expression{Func{TGridItem, TPro}})"/>.
    /// </remarks>
    public bool IsSortable
    {
        get => column.IsSortable;
        set
        {
            column.IsSortable = value;
            CheckSortability();
        }
    }

    /// <summary>
    /// Gets or sets a value indicating whether this column can be sorted with other sortable columns.
    /// </summary>
    /// <remarks>
    /// If this property is set to <c>true</c> and the <see cref="IsSortable"/> property is also set to <c>true</c>, this column can be sorted with other sortable columns.
    /// </remarks>
    public bool MultipleSortingAllowed { get => column.MultipleSortingAllowed; set => column.MultipleSortingAllowed = value; }

    /// <summary>
    /// Sets the property expression and property type for the column using a lambda expression.
    /// </summary>
    /// <typeparam name="TPro">The type of the property to use.</typeparam>
    /// <param name="expression">The lambda expression representing the property to use.</param>
    public void SetPropertyExpressionAndType<TPro>(Expression<Func<TGridItem, TPro>> expression)
    {
        column.SetPropertyExpressionAndTypet(expression);
    }

    /// <summary>
    /// Adds or updates a filter for this column.
    /// If the filter expression is null, the existing filter for this column is removed.
    /// Otherwise, the existing filter for this column is updated or added to the list of filters.
    /// </summary>
    public void ApplyColumnFilter(Expression<Func<TGridItem, bool>>? expression)
    {
        column.Grid.ApplyColumnFilter(expression, column);
    }

    /// <summary>
    /// Sorts grid data.
    /// </summary>
    public void ApplySort()
    {
        if (CheckSortability())
        {
            column.ApplySort();
        }
        else
        {
            throw new ColumnCException();
        }
    }

    /// <summary> 
    /// Get the sort direction of the column.
    /// </summary>
    /// <returns>The sort direction or null if the column is not sortable</returns>
    public SortDirection? GetSortDirection()
    {
        return column.Grid.GetSortDirection(column);
    }

    /// <summary> 
    /// Checks if the column is sortable and updates its sort state accordingly.
    /// </summary> 
    /// <returns>Returns true if the column is sortable, false otherwise.</returns>
    public bool CheckSortability()
    {
        if (IsSortable && column.PropertyExpression != null)
        {
            column.Grid.ColumnSortDirectionsAdding(column);
            return true;
        }
        else
        {
            return false;
        }
    }
}
