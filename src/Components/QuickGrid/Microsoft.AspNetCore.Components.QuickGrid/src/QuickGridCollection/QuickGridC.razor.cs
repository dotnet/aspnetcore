// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components.QuickGrid.QuickGridCollection.Columns;
using Microsoft.AspNetCore.Components.QuickGrid.QuickGridCollection.Infrastructure;
using System.Linq;
using System.Linq.Expressions;

namespace Microsoft.AspNetCore.Components.QuickGrid.QuickGridCollection;

/// <summary>
/// Blazor component representing a generic data grid.
/// Allows displaying, filtering and sorting data of type <c>TGridItem</c>.
/// </summary>
/// <typeparam name="TGridItem">The type of data items displayed in the grid.</typeparam>
[CascadingTypeParameter(nameof(TGridItem))]
public partial class QuickGridC<TGridItem> : ComponentBase
{
    /// <summary>
    /// Collection of items currently displayed in the grid.
    /// </summary>
    private ICollection<TGridItem> _currentItems = Array.Empty<TGridItem>();

    /// <summary>
    /// Last value assigned to the <see cref="QuickGridC{TGridItem}.Items"/> property.
    /// </summary>
    private ICollection<TGridItem>? _lastAssignedItems;
    private bool _collectingColumns;

    // The <see cref="QuickGridC{TGridItem}.CssClassAndStyle"/> field is an instance of the <see cref="GridHtmlCssManager"/> class that allows managing the CSS classes and styles of the grid's HTML elements. This class contains dictionaries associating each HTML element with its CSS class or style. These dictionaries are initialized in the class constructor with default values.

    /// <summary>
    /// Object for managing the CSS classes and styles of the grid's HTML elements.
    /// </summary>
    private GridHtmlCssManager cssClassAndStyle = new();

    /// <summary>
    /// Dictionary associating each sortable column with its current sort direction.
    /// </summary>
    private readonly Dictionary<ColumnCBase<TGridItem>, SortDirection> columnSortDirections = new();

    /// <summary>
    /// List of columns to sort
    /// </summary>
    private readonly List<KeyValuePair<ColumnCBase<TGridItem>, (SortDirection, Expression<Func<TGridItem, object?>>)>> columnsSorted = new();

    /// <summary>
    /// Dictionary associating each sort direction with its corresponding CSS class.
    /// </summary>
    private readonly Dictionary<SortDirection, string> sortDirectionCssClasses;
    /// <summary>
    /// Dictionary associating each sort direction with its corresponding CSS style.
    /// </summary>
    private readonly Dictionary<SortDirection, string> sortDirectionCssStyles;

    /// <summary>
    /// List of filters applied to the grid data.
    /// </summary>
    private readonly List<KeyValuePair<ColumnCBase<TGridItem>, Expression<Func<TGridItem, bool>>>> columnFilters = new();

    /// <summary>
    /// Object containing filter expressions and sort expressions for each column.
    /// </summary>
    private GridFilteringAndSorting<TGridItem> _gridFilteringSorting;

    private readonly List<ColumnCBase<TGridItem>> _columns;
    private readonly RenderFragment _ColumnHeaders;
    private readonly RenderFragment _CellText;
    private readonly InternalGridContext<TGridItem> _internalGridContext;
    /// <summary>
    /// 
    /// </summary>
    public QuickGridC()
    {
        _internalGridContext = new(this);
        _columns = new();
        _ColumnHeaders = RenderColumnHeaders;
        _CellText = RenderCellText;

        sortDirectionCssClasses = new()
        {
            { SortDirection.Ascending, CssClassAndStyle[CssClass.Column_Sort_i_i_SortAsc] },
            { SortDirection.Descending, CssClassAndStyle[CssClass.Column_Sort_i_i_SortDesc] },
            { SortDirection.Default, CssClassAndStyle[CssClass.Column_Sort_i_i_Sortdefault] }
        };
        sortDirectionCssStyles = new()
        {
            { SortDirection.Ascending, CssClassAndStyle[CssStyle.Column_Sort_i_i_SortAsc] },
            { SortDirection.Descending, CssClassAndStyle[CssStyle.Column_Sort_i_i_SortDesc] },
            { SortDirection.Default, CssClassAndStyle[CssStyle.Column_Sort_i_i_Sortdefault] }
        };
    }
    /// <summary>
    /// 
    /// </summary>
    [Parameter] public RenderFragment? ChildContent { get; set; }

    /// <summary>
    /// Collection of items to display in the grid.
    /// </summary>
    [Parameter, EditorRequired] public ICollection<TGridItem> Items { get; set; } = null!;

    /// <summary>
    /// Callback called when a row in the grid is selected.
    /// </summary>
    [Parameter] public EventCallback<TGridItem> RowSelected { get; set; }

    /// <summary>
    /// Callback called when a filter or sort is changed.        
    /// </summary>
    [Parameter] public EventCallback<GridFilteringAndSorting<TGridItem>> FilterSortChanged { get; set; }

    /// <summary>
    /// Object for managing the CSS classes and styles of the grid's HTML elements.
    /// </summary>
    [Parameter] public GridHtmlCssManager CssClassAndStyle { get => cssClassAndStyle; set => cssClassAndStyle = value; }

    internal bool IsFirstRender { get; set; } = true;

    /// <inheritdoc/>
    protected override Task OnParametersSetAsync()
    {
        var newAssignedIems = Items;
        var dataSourceHasChanged = newAssignedIems != _lastAssignedItems;
        if (dataSourceHasChanged)
        {
            _lastAssignedItems = newAssignedIems;
        }
        return dataSourceHasChanged ? RefreshDataAsync() : Task.CompletedTask;
    }

    /// <summary>
    /// Adds a column to the grid.
    /// If the column is sortable, it is added to the <see cref="QuickGridC{TGridItem}.columnSortDirections"/> dictionary with a default sort direction.
    /// </summary>
    internal void AddColumn(ColumnCBase<TGridItem> column)
    {
        if (_collectingColumns)
        {
            ColumnSortDirectionsAdding(column);
            _columns.Add(column);
        }
    }
    internal void ColumnSortDirectionsAdding(ColumnCBase<TGridItem> column)
    {
        if (column.PropertyExpression != null && column.IsSortable)
        {
            columnSortDirections.TryAdd(column, SortDirection.Default);
        }
    }
    internal void StateChanged() => StateHasChanged();
    private void StartCollectingColumns()
    {
        _collectingColumns = true;
    }

    private void FinishCollectingColumns()
    {
        _collectingColumns = false;
        IsFirstRender = false;
    }

    // The filter field is a list of key-value pairs associating each filterable column with its filter expression. The AddOrMoveFilter method is used to add or update a filter for a specified column. If the filter expression passed as a parameter is null, the existing filter for this column is removed from the filter list. Otherwise, the existing filter for this column is updated or added to the filter list.

    /// <summary>
    /// Adds or updates a filter for a specified column.
    /// If the filter expression is null, the existing filter for this column is removed.
    /// Otherwise, the existing filter for this column is updated or added to the list of filters.
    /// </summary>
    internal async void ApplyColumnFilter(Expression<Func<TGridItem, bool>>? expression, ColumnCBase<TGridItem> column)
    {
        columnFilters.RemoveAll(e => e.Key == column);
        if (expression != null)
        {
            columnFilters.Add(new(column, expression));
        }

        _gridFilteringSorting.FilterExpressions = columnFilters.Select(e => e.Value).ToArray();
        await FilterSortChanged.InvokeAsync(_gridFilteringSorting);
    }

    internal SortDirection? GetSortDirection(ColumnCBase<TGridItem> column)
    {
        var hasValue = columnSortDirections.TryGetValue(column, out var sortDirection);
        if (hasValue)
        {
            return sortDirection;
        }

        return null;
    }

    /// <summary>
    /// Sorts the grid data based on the specified column.
    /// Updates the <see cref="QuickGridC{TGridItem}.columnsSorted"/> list based on the new sort direction.
    /// Also updates the sort direction in the <see cref="QuickGridC{TGridItem}.columnSortDirections"/> dictionary.
    /// </summary>
    internal async void ApplySort(ColumnCBase<TGridItem> column)
    {
        var hasValue = columnSortDirections.TryGetValue(column, out SortDirection sortDirection);
        if (hasValue)
        {
            if (sortDirection == SortDirection.Ascending || sortDirection == SortDirection.Descending)
            {
                columnsSorted.RemoveAll(e => e.Key == column);
            }
            else if (!column.MultipleSortingAllowed)
            {
                columnsSorted.RemoveAll(e => e.Key == column || e.Key != column);
                foreach (var keyValue in columnSortDirections)
                {
                    if (keyValue.Key != column)
                    {
                        columnSortDirections[keyValue.Key] = SortDirection.Default;
                    }
                }
            }
            var newSortDirection = sortDirection switch
            {
                SortDirection.Default => SortDirection.Ascending,
                SortDirection.Ascending => SortDirection.Descending,
                SortDirection.Descending => SortDirection.Default,
                _ => throw new NotSupportedException($"Unknown sort direction {sortDirection}"),
            };
            if (newSortDirection == SortDirection.Ascending)
            {
                columnsSorted.Add(new(column, (SortDirection.Ascending, column.PropertyExpression!)));
            }
            else if (newSortDirection == SortDirection.Descending)
            {
                columnsSorted.Add(new(column, (SortDirection.Descending, column.PropertyExpression!)));
            }
            columnSortDirections[column] = newSortDirection;
        }
        else
        {
            throw new QuickGridCException();
        }

        _gridFilteringSorting.SortExpressions = columnsSorted.Select((e, index) =>
        {
            (var sort, var exp) = e.Value;
            if (index == 0)
            {
                if (sort == SortDirection.Ascending)
                {
                    return (SortedLinq.OrderBy, exp);
                }
                else
                {
                    return (SortedLinq.OrderByDescending, exp);
                }
            }
            else
            {
                if (sort == SortDirection.Ascending)
                {
                    return (SortedLinq.ThenBy, exp);
                }
                else
                {
                    return (SortedLinq.ThenByDescending, exp);
                }
            }
        }).ToArray();

        await FilterSortChanged.InvokeAsync(_gridFilteringSorting);

    }

    /// <summary>
    /// Gets the CSS class corresponding to the sort direction of a column.
    /// Uses the <see cref="QuickGridC{TGridItem}.sortDirectionCssClasses"/> dictionary to associate each sort direction with its corresponding CSS class.
    /// </summary>
    internal string GetSortCssClass(ColumnCBase<TGridItem> column)
    {
        if (GetSortDirection(column) is SortDirection sortDirection)
        {
            return sortDirectionCssClasses[sortDirection];
        }
        return CssClassAndStyle[CssClass.Column_Sort_i_i_SortNot];
    }

    /// <summary>
    /// Gets the CSS Style corresponding to the sort direction of a column.
    /// Uses the <see cref="QuickGridC{TGridItem}.sortDirectionCssClasses"/> dictionary to associate each sort direction with its corresponding CSS class.
    /// </summary>
    internal string GetSortCssStyle(ColumnCBase<TGridItem> column)
    {
        if (GetSortDirection(column) is SortDirection sortDirection)
        {
            return sortDirectionCssStyles[sortDirection];
        }
        return CssClassAndStyle[CssStyle.Column_Sort_i_i_SortNot];
    }

    /// <summary>
    /// Get aria-sort value for html element
    /// </summary>
    /// <param name="column">instance de la colonne</param>
    /// <returns xml:lang="fr">La valeur en <c>string</c> selon étal de la colonne</returns>
    private string AriaSortValue(ColumnCBase<TGridItem> column)
    {
        var hasValue = columnSortDirections.TryGetValue(column, out SortDirection sortDirection);
        if (hasValue)
        {
            return sortDirection switch
            {
                SortDirection.Default => "other",
                _ => sortDirection.ToString().ToLower(System.Globalization.CultureInfo.CurrentCulture)
            };
        }
        else
        {
            return "none";
        }
    }

    /// <summary>
    /// Invokes the <see cref="QuickGridC{TGridItem}.RowSelected"/> callback with the selected row item.
    /// </summary>
    private void HandleRowSelection(TGridItem item)
    {
        RowSelected.InvokeAsync(item);
    }

    /// <summary>
    /// Updates the items displayed in the grid.
    /// If the last value assigned to the <see cref="QuickGridC{TGridItem}.Items"/> property is not null,
    /// the currently displayed items are updated with this value.
    /// Otherwise, the currently displayed items are set to an empty array.
    /// </summary>
    private Task RefreshDataAsync()
    {
        if (_lastAssignedItems != null)
        {
            _currentItems = _lastAssignedItems;
        }
        else
        {
            _currentItems = Array.Empty<TGridItem>();
        }

        return Task.CompletedTask;
    }
}
