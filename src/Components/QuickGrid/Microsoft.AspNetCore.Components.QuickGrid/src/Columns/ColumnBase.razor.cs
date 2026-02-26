// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components.QuickGrid.Infrastructure;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web.Virtualization;

namespace Microsoft.AspNetCore.Components.QuickGrid;

/// <summary>
/// An abstract base class for columns in a <see cref="QuickGrid{TGridItem}"/>.
/// </summary>
/// <typeparam name="TGridItem">The type of data represented by each row in the grid.</typeparam>
public abstract partial class ColumnBase<TGridItem>
{
    [CascadingParameter] internal InternalGridContext<TGridItem> InternalGridContext { get; set; } = default!;

    /// <summary>
    /// Title text for the column. This is rendered automatically if <see cref="HeaderTemplate" /> is not used.
    /// </summary>
    [Parameter] public string? Title { get; set; }

    /// <summary>
    /// An optional CSS class name. If specified, this is included in the class attribute of table header and body cells
    /// for this column.
    /// </summary>
    [Parameter] public string? Class { get; set; }

    /// <summary>
    /// If specified, controls the justification of table header and body cells for this column.
    /// </summary>
    [Parameter] public Align Align { get; set; }

    /// <summary>
    /// An optional template for this column's header cell. If not specified, the default header template
    /// includes the <see cref="Title" /> along with any applicable sort indicators and options buttons.
    /// </summary>
    [Parameter] public RenderFragment<ColumnBase<TGridItem>>? HeaderTemplate { get; set; }

    /// <summary>
    /// If specified, indicates that this column has this associated options UI. A button to display this
    /// UI will be included in the header cell by default.
    ///
    /// If <see cref="HeaderTemplate" /> is used, it is left up to that template to render any relevant
    /// "show options" UI and invoke the grid's <see cref="QuickGrid{TGridItem}.ShowColumnOptionsAsync(ColumnBase{TGridItem})" />).
    /// </summary>
    [Parameter] public RenderFragment? ColumnOptions { get; set; }

    /// <summary>
    /// Indicates whether the data should be sortable by this column.
    ///
    /// The default value may vary according to the column type (for example, a <see cref="TemplateColumn{TGridItem}" />
    /// is sortable by default if any <see cref="TemplateColumn{TGridItem}.SortBy" /> parameter is specified).
    /// </summary>
    [Parameter] public bool? Sortable { get; set; }

    /// <summary>
    /// Specifies sorting rules for a column.
    /// </summary>
    public abstract GridSort<TGridItem>? SortBy { get; set; }

    /// <summary>
    /// Indicates which direction to sort in
    /// if <see cref="IsDefaultSortColumn"/> is true.
    /// </summary>
    [Parameter] public SortDirection InitialSortDirection { get; set; } = default;

    /// <summary>
    /// Indicates whether this column should be sorted by default.
    /// </summary>
    [Parameter] public bool IsDefaultSortColumn { get; set; } = false;

    /// <summary>
    /// If specified, virtualized grids will use this template to render cells whose data has not yet been loaded.
    /// </summary>
    [Parameter] public RenderFragment<PlaceholderContext>? PlaceholderTemplate { get; set; }

    /// <summary>
    /// Gets a reference to the enclosing <see cref="QuickGrid{TGridItem}" />.
    /// </summary>
    public QuickGrid<TGridItem> Grid => InternalGridContext.Grid;

    /// <summary>
    /// Overridden by derived components to provide rendering logic for the column's cells.
    /// </summary>
    /// <param name="builder">The current <see cref="RenderTreeBuilder" />.</param>
    /// <param name="item">The data for the row being rendered.</param>
    protected internal abstract void CellContent(RenderTreeBuilder builder, TGridItem item);

    /// <summary>
    /// Gets or sets a <see cref="RenderFragment" /> that will be rendered for this column's header cell.
    /// This allows derived components to change the header output. However, derived components are then
    /// responsible for using <see cref="HeaderTemplate" /> within that new output if they want to continue
    /// respecting that option.
    /// </summary>
    protected internal RenderFragment HeaderContent { get; protected set; }

    /// <summary>
    /// Get a value indicating whether this column should act as sortable if no value was set for the
    /// <see cref="ColumnBase{TGridItem}.Sortable" /> parameter. The default behavior is not to be
    /// sortable unless <see cref="ColumnBase{TGridItem}.Sortable" /> is true.
    ///
    /// Derived components may override this to implement alternative default sortability rules.
    /// </summary>
    /// <returns>True if the column should be sortable by default, otherwise false.</returns>
    protected virtual bool IsSortableByDefault() => false;

    /// <summary>
    /// Constructs an instance of <see cref="ColumnBase{TGridItem}" />.
    /// </summary>
    public ColumnBase()
    {
        HeaderContent = RenderDefaultHeaderContent;
    }
}
