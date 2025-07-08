namespace Microsoft.AspNetCore.Components.QuickGrid;

/// <summary>
/// Provides information about the column that has sorting applied.
/// </summary>
/// <typeparam name="TGridItem">The type of data represented by each row in the grid.</typeparam>
public class SortColumn<TGridItem>
{
    /// <summary>
    /// The column that has sorting applied.
    /// </summary>
    public ColumnBase<TGridItem>? Column { get; init; }

    /// <summary>
    /// Whether or not the sort is ascending.
    /// </summary>
    public bool Ascending { get; internal set; }
}
