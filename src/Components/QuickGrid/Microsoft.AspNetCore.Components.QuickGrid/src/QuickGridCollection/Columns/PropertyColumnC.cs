// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components.QuickGrid.QuickGridCollection.Columns;
using Microsoft.AspNetCore.Components.Rendering;
using System.Linq.Expressions;

namespace Microsoft.AspNetCore.Components.QuickGrid.QuickGridCollection;

/// <summary>
/// The <see cref="PropertyColumnC{TGridItem, TProp}"/> class inherits from the <see cref="ColumnCBase{TGridItem}"/> class and represents a property column in a table.
/// </summary>
///<typeparam name="TGridItem">The type of data items displayed in the grid.</typeparam>
///<typeparam name="TProp">The type of the property.</typeparam>
public partial class PropertyColumnC<TGridItem, TProp> : ColumnCBase<TGridItem>
{
    /// <summary>
    /// Function to get the cell text.
    /// </summary>
    private Func<TGridItem, string?>? _cellTextFunc;

    /// <summary>
    /// Reference to the last instance of <see cref="PropertyColumnC{TGridItem, TProp}.DisplayFormat"/> assigned to this variable.
    /// </summary>
    private string? lastDisplayForma;

    /// <summary>
    /// Expression to get the property to display in the column.
    /// </summary>
    [Parameter, EditorRequired] public Expression<Func<TGridItem, TProp>> Property { get; set; } = default!;

    /// <summary>
    /// Format to use for displaying the property.
    /// </summary>
    [Parameter] public string? DisplayFormat { get; set; }

    /// <summary>
    /// Indicates whether the column is sortable.
    /// </summary>
    [Parameter] public new bool IsSortable { get => isSortable; set => isSortable = value; }

    /// <summary>
    /// Gets or sets a value indicating whether this column can be sorted with other sortable columns.
    /// </summary>
    /// <remarks>
    /// If this property is set to <c>true</c> and the <see cref="IsSortable"/> property is also set to <c>true</c>, this column can be sorted with other sortable columns.
    /// </remarks>
    [Parameter] public new bool MultipleSortingAllowed { get => multipleSortingAllowed; set => multipleSortingAllowed = value; }

    /// <summary>
    /// Indicates whether the column has filter options.
    /// </summary>
    ///<remarks>
    /// Note: if <see cref="PropertyColumnC{TGridItem, TProp}.HasAdvancedFilterOptions"/> is set to <c>true</c>, <see cref="PropertyColumnC{TGridItem, TProp}.HasFilterOptions"/> will be set to <c>false</c>.
    ///</remarks>
    [Parameter] public bool HasFilterOptions { get => hasFilterOptions; set => hasFilterOptions = value; }

    /// <summary>
    /// Indicates whether the column has advanced filter options.
    /// </summary>
    ///<remarks>
    /// Note: if <see cref="PropertyColumnC{TGridItem, TProp}.HasAdvancedFilterOptions"/> is set to <c>true</c>, <see cref="PropertyColumnC{TGridItem, TProp}.HasFilterOptions"/> will be set to <c>false</c>.
    ///</remarks>
    [Parameter] public bool HasAdvancedFilterOptions { get => hasAdvancedFilterOptions; set => hasAdvancedFilterOptions = value; }

    /// <summary>
    /// Maximum number of filters to apply for this column. The default value is 5 and The minimum value is 2.
    /// </summary>
    /// <remarks> 
    /// This property is used if <see cref="PropertyColumnC{TGridItem, TProp}.HasAdvancedFilterOptions"/> is set to true
    ///</remarks> 
    [Parameter]
    public new int MaxFilters
    {
        get => maxFilters;
        set
        {
            if (value < 2)
            {
                maxFilters = 2;
            }
            else
            {
                maxFilters = value;
            }
        }
    }
    /// <inheritdoc/>
    protected override void OnParametersSet()
    {
        var memberExpression = Property.Body as MemberExpression;

        if (HasAdvancedFilterOptions)
        {
            hasFilterOptions = false;
        }

        var IsNewProperty = InternalGridContext.Grid.IsFirstRender;
        if (IsNewProperty)
        {
            if (memberExpression != null)
            {
                SetPropertyExpressionAndTypet(memberExpression);
            }
        }
        if (IsNewProperty || lastDisplayForma != DisplayFormat)
        {
            lastDisplayForma = DisplayFormat;
            var compiledPropertyExpression = Property.Compile() ?? throw new ArgumentNullException();

            if (string.IsNullOrEmpty(DisplayFormat) && memberExpression is not null)
            {
                GetDisplayFormatFromDataAnnotations(memberExpression);
            }

            if (!string.IsNullOrEmpty(DisplayFormat))
            {
                var nullableUnderlyingTypeOrNull = Nullable.GetUnderlyingType(typeof(TProp));
                if (!typeof(IFormattable).IsAssignableFrom(nullableUnderlyingTypeOrNull ?? typeof(TProp)))
                {
                    throw new InvalidOperationException($"A '{nameof(DisplayFormat)}' parameter was supplied, but the type '{typeof(TProp)}' does not implement '{typeof(IFormattable)}'.");
                }

                _cellTextFunc = (TGridItem item) => ((IFormattable?)compiledPropertyExpression(item))?.ToString(DisplayFormat, null);
            }
            else
            {
                _cellTextFunc = (TGridItem item) => compiledPropertyExpression(item)?.ToString();
            }
        }

        if (Title is null && memberExpression is not null)
        {
            GetTitleFromDataAnnotations(memberExpression);
            Title ??= memberExpression.Member.Name;
        }
        if (IsNewProperty)
        {
            AddColumn();
        }
    }

    /// <inheritdoc/>
    protected internal override void CellContent(RenderTreeBuilder builder, TGridItem item)
    => builder.AddContent(0, _cellTextFunc!(item));

    partial void GetTitleFromDataAnnotations(MemberExpression memberExpression);

    partial void GetDisplayFormatFromDataAnnotations(MemberExpression memberExpression);

}
