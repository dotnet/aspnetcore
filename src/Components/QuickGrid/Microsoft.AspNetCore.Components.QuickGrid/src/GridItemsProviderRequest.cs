// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq;

namespace Microsoft.AspNetCore.Components.QuickGrid;

/// <summary>
/// Parameters for data to be supplied by a <see cref="QuickGrid{TGridItem}"/>'s <see cref="QuickGrid{TGridItem}.ItemsProvider"/>.
/// </summary>
/// <typeparam name="TGridItem">The type of data represented by each row in the grid.</typeparam>
public readonly struct GridItemsProviderRequest<TGridItem>
{
    /// <summary>
    /// The zero-based index of the first item to be supplied.
    /// </summary>
    public int StartIndex { get; init; }

    /// <summary>
    /// If set, the maximum number of items to be supplied. If not set, the maximum number is unlimited.
    /// </summary>
    public int? Count { get; init; }

    /// <summary>
    /// Specifies which columns are currently being sorted.
    /// 
    /// Rather than inferring the sort rules manually, you should normally call either <see cref="ApplySorting(IQueryable{TGridItem})"/>
    /// or <see cref="GetSortByProperties"/>, since they also account for <see cref="SortColumn{TGridItem}.Column"/> 
    /// and <see cref="SortColumn{TGridItem}.Ascending"/> automatically.
    /// </summary>
    public IReadOnlyList<SortColumn<TGridItem>> SortColumns { get; init; }

    /// <summary>
    /// Specifies which column represents the sort order.
    ///
    /// Rather than inferring the sort rules manually, you should normally call either <see cref="ApplySorting(IQueryable{TGridItem})"/>
    /// or <see cref="GetSortByProperties"/>, since they also account for <see cref="SortByColumn" /> and <see cref="SortByAscending" /> automatically.
    /// </summary>
    [Obsolete("Use " + nameof(SortColumns) + " instead.")]
    public ColumnBase<TGridItem>? SortByColumn
    {
        get => _sortByColumn;
        init => _sortByColumn = value;
    }

    private readonly ColumnBase<TGridItem>? _sortByColumn;

    /// <summary>
    /// Specifies the current sort direction.
    ///
    /// Rather than inferring the sort rules manually, you should normally call either <see cref="ApplySorting(IQueryable{TGridItem})"/>
    /// or <see cref="GetSortByProperties"/>, since they also account for <see cref="SortByColumn" /> and <see cref="SortByAscending" /> automatically.
    /// </summary>
    [Obsolete("Use " + nameof(SortColumns) + " instead.")]
    public bool SortByAscending
    {
        get => _sortByAscending;
        init => _sortByAscending = value;
    }

    private readonly bool _sortByAscending;

    /// <summary>
    /// A token that indicates if the request should be cancelled.
    /// </summary>
    public CancellationToken CancellationToken { get; init; }

    internal GridItemsProviderRequest(
        int startIndex, int? count, IReadOnlyList<SortColumn<TGridItem>> sortColumns, CancellationToken cancellationToken)
    {
        StartIndex = startIndex;
        Count = count;
        SortColumns = sortColumns;

        if (sortColumns.Any())
        {
            var sortColumn = sortColumns[0];
            _sortByColumn = sortColumn.Column;
            _sortByAscending = sortColumn.Ascending;
        }

        CancellationToken = cancellationToken;
    }

    /// <summary>
    /// Applies the request's sorting rules to the supplied <see cref="IQueryable{TGridItem}"/>.
    /// </summary>
    /// <param name="source">An <see cref="IQueryable{TGridItem}"/>.</param>
    /// <returns>A new <see cref="IQueryable{TGridItem}"/> representing the <paramref name="source"/> with sorting rules applied.</returns>
    public IQueryable<TGridItem> ApplySorting(IQueryable<TGridItem> source)
    {
        for (var i = 0; i < SortColumns.Count; i++)
        {
            var sortColumn = SortColumns[i];

            if (sortColumn.Column?.SortBy != null)
            {
                source = sortColumn.Column.SortBy.Apply(source, sortColumn.Ascending, i == 0);
            }
        }

        return source;
    }

    /// <summary>
    /// Produces a collection of (property name, direction) pairs representing the sorting rules.
    /// </summary>
    /// <returns>A collection of (property name, direction) pairs representing the sorting rules</returns>
    [Obsolete("Use " + nameof(GetSortColumnProperties) + " instead.")]
    public IReadOnlyCollection<SortedProperty> GetSortByProperties() =>
            SortByColumn?.SortBy?.ToPropertyList(SortByAscending) ?? Array.Empty<SortedProperty>();

    /// <summary>
    /// Produces a collection of collections representing the applied sort rules for the grid.
    /// </summary>
    /// <remarks>
    /// Each item in the returned sequence is a collection of property name and sort direction pairs that define one sorting expression.
    /// The sequence reflects the sort precedence (first, second, etc.).
    /// </remarks>
    /// <returns>
    /// An <see cref="IEnumerable{T}"/> of <see cref="IReadOnlyCollection{T}"/> of <see cref="SortedProperty"/> representing the full sorting expression.
    /// </returns>
    public IEnumerable<IReadOnlyCollection<SortedProperty>> GetSortColumnProperties()
    {
        foreach (var sortColumn in SortColumns)
        {
            yield return sortColumn.Column?.SortBy?.ToPropertyList(sortColumn.Ascending) ?? [];
        }
    }
}
