// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components.QuickGrid.QuickGridCollection.Infrastructure;
using Microsoft.AspNetCore.Components.Rendering;
using System.Linq.Expressions;

namespace Microsoft.AspNetCore.Components.QuickGrid.QuickGridCollection.Columns;

/// <summary>
/// Represents a column in a data grid.
/// It contains properties and methods to manage column options,
/// such as visibility of options, ability to sort and filter data,
/// as well as property expressions and property types for the column.
/// </summary>
/// <typeparam name="TGridItem">The type of data items displayed in the grid.</typeparam>
[CascadingTypeParameter(nameof(TGridItem))]
public abstract partial class ColumnCBase<TGridItem> : ComponentBase
{
    /// <summary>
    /// Reference to the last instance of <see cref="ColumnCBase{TGridItem}"/> assigned to this variable.
    /// </summary>
    private ColumnCBase<TGridItem>? _lastAssignedColumn;

    /// <summary>
    /// Indicates whether column options are shown or hidden.
    /// </summary>
    private bool isOptionVisible;

    /// <summary>
    /// Indicates whether the column is sortable.
    /// </summary>
    /// <remarks>
    /// if <see cref="isSortable"/> is set to <c>true</c> then <see cref="ColumnCBase{TGridItem}.PropertyExpression"/> must not be <c>null</c>
    /// </remarks>
    protected bool isSortable;

    /// <summary>
    /// Gets or sets a value indicating whether this column can be sorted with other sortable columns.
    /// </summary>
    /// <remarks>
    /// If this property is set to <c>true</c> and the <see cref="IsSortable"/> property is also set to <c>true</c>, this column can be sorted with other sortable columns.
    /// </remarks>
    protected bool multipleSortingAllowed;

    /// <summary>
    /// Maximum number of filters to apply for this column. The default value is 5.
    /// </summary>
    protected int maxFilters = 5;

    /// <summary>
    /// Indicates whether the column has advanced filter options.
    /// </summary>
    /// <remarks>
    /// if <see cref="hasAdvancedFilterOptions"/> is set to <c>true</c> then <see cref="ColumnCBase{TGridItem}.PropertyExpression"/> must not be <c>null</c>
    /// </remarks>
    protected bool hasAdvancedFilterOptions;

    /// <summary>
    /// Indicates whether the column has filter options.
    /// </summary>
    /// /// <remarks>
    /// if <see cref="hasFilterOptions"/> is set to <c>true</c> then <see cref="ColumnCBase{TGridItem}.PropertyExpression"/> must not be <c>null</c>
    /// </remarks>
    protected bool hasFilterOptions;

    /// <summary>
    /// Indicates whether the column options are applied.
    /// </summary>
    private bool optionApplied;

    /// <summary>
    /// Type of the column property.
    /// </summary>
    protected Type? typeOfProperty;

    /// <summary>
    /// Property expression for the column.
    /// </summary>
    protected Expression<Func<TGridItem, object?>>? propertyExpression;
    /// <summary>
    /// 
    /// </summary>
    protected ColumnCBase()
    {
        HeaderContent = RenderDefaultHeaderContent;
        SortContent = RenderSortContent;
        OptionsContent = RenderOptionsContent;
    }

    /// <summary>
    /// Internal grid context.
    /// </summary>
    [CascadingParameter] internal InternalGridContext<TGridItem> InternalGridContext { get; set; } = default!;

    /// <summary>
    /// Column title.
    /// </summary>
    [Parameter] public string? Title { get; set; }

    /// <summary>
    /// Custom header template for the column.
    /// </summary>
    [Parameter] public RenderFragment<HeaderTemplateContext<TGridItem>>? HeaderTemplate { get; set; }

    /// <summary>
    /// Custom column options.
    /// </summary>
    [Parameter] public RenderFragment<ColumnOptionsContext<TGridItem>>? ColumnOptions { get; set; }

    /// <summary>
    /// Column header content.
    /// </summary>
    protected internal RenderFragment HeaderContent { get; protected set; }

    /// <summary>
    /// Column header sort content.
    /// </summary>
    protected internal RenderFragment SortContent { get; protected set; }

    /// <summary>
    /// Column header options menu content.
    /// </summary>
    protected internal RenderFragment OptionsContent { get; protected set; }

    /// <summary>
    /// Property expression for the column.
    /// </summary>
    internal Expression<Func<TGridItem, object?>>? PropertyExpression => propertyExpression;

    /// <summary>
    /// Type of the column property.
    /// </summary>
    internal Type? TypeOfProperty => typeOfProperty;

    /// <summary>
    /// Indicates whether the column is sortable.
    /// </summary>
    /// /// <remarks>
    /// if <see cref="IsSortable"/> is set to <c>true</c> then <see cref="ColumnCBase{TGridItem}.PropertyExpression"/> must not be <c>null</c>
    /// </remarks>
    internal bool IsSortable { get => isSortable; set => isSortable = value; }

    /// <summary>
    /// Gets or sets a value indicating whether this column can be sorted with other sortable columns.
    /// </summary>
    /// <remarks>
    /// If this property is set to <c>true</c> and the <see cref="IsSortable"/> property is also set to <c>true</c>, this column can be sorted with other sortable columns.
    /// </remarks>
    internal bool MultipleSortingAllowed { get => multipleSortingAllowed; set => multipleSortingAllowed = value; }

    /// <summary>
    /// instance of <see cref="QuickGridC{TGridItem}"/>
    /// </summary>
    /// <summary xml:lang="fr">
    /// instance de <see cref="QuickGridC{TGridItem}"/>
    /// </summary>
    internal QuickGridC<TGridItem> Grid => InternalGridContext.Grid;

    /// <summary>
    /// Object for managing CSS classes and styles of HTML elements in the grid.
    /// </summary>
    internal GridHtmlCssManager CssClassAndStyle => Grid.CssClassAndStyle;

    /// <summary>
    /// Indicates whether the column options are applied.
    /// </summary>
    internal bool OptionApplied { get => optionApplied; set => optionApplied = value; }

    /// <summary>
    /// Maximum number of filters to apply for this column. The default value is 5, The minimum value is 2
    /// </summary>
    /// <remarks> This property is used by <see cref="MenuAdvancedFilter{TGridItem}"/> </remarks>
    internal int MaxFilters
    {
        get => maxFilters; set
        {
            if (value < 1)
            {
                maxFilters = 2;
            }
            else
            {
                maxFilters = value;
            }
        }
    }

    /// <summary>
    /// Indicates whether column options are shown or hidden.
    /// </summary>
    internal bool IsOptionVisible { get => isOptionVisible; set => isOptionVisible = value; }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="builder"></param>
    /// <param name="item"></param>
    protected internal abstract void CellContent(RenderTreeBuilder builder, TGridItem item);

    /// <summary>
    /// Adds a column to the grid.
    /// </summary>
    protected void AddColumn()
    {
        Grid.AddColumn(_lastAssignedColumn = this);
    }

    /// <summary>
    /// Sorts the grid data.
    /// Updates the list of sorted columns in the grid based on the new sort direction.
    /// Also updates the sort direction in the dictionary of sort directions for each column in the grid.
    /// </summary>
    internal void ApplySort()
    {
        Grid.ApplySort(_lastAssignedColumn!);
    }

    /// <summary>
    /// Sets the property expression and property type for the column.
    /// </summary>
    /// <param name="memberExp">Member expression to use.</param>
    internal void SetPropertyExpressionAndTypet(MemberExpression memberExp)
    {
        var parameterExp = memberExp.Expression as ParameterExpression;
        propertyExpression = Expression.Lambda<Func<TGridItem, object?>>(Expression.Convert(memberExp, typeof(object)), parameterExp!);
        typeOfProperty = Nullable.GetUnderlyingType(memberExp.Type) ?? memberExp.Type;
    }

    /// <summary>
    /// Sets the property expression and property type for the column using a lambda expression.
    /// </summary>
    /// <typeparam name="TPro">The type of the property to use.</typeparam>
    /// <param name="expression">The lambda expression representing the property to use.</param>
    internal void SetPropertyExpressionAndTypet<TPro>(Expression<Func<TGridItem, TPro>> expression)
    {
        if (expression.Body is MemberExpression memberExpression)
        {
            SetPropertyExpressionAndTypet(memberExpression);
        }
    }

    /// <summary>
    /// Shows or hides column options.
    /// </summary>
    private void ToggleColumnOptionsVisibility()
    {
        isOptionVisible = !isOptionVisible;
        Grid.StateChanged();
    }

    /// <summary>
    /// Resolves the CSS class for column options.
    /// </summary>
    private string GetColumnOptionCssClass()
    {
        if (ColumnOptions != null)
        {
            return optionApplied ? CssClassAndStyle[CssClass.Column_Options_i_i_ColumnOptionActive] :
                CssClassAndStyle[CssClass.Column_Options_i_i_ColumnOptionNotActive];
        }
        else if (hasAdvancedFilterOptions)
        {
            return optionApplied ? CssClassAndStyle[CssClass.Column_Options_i_i_MenuAdvancedFilterActive] :
                CssClassAndStyle[CssClass.Column_Options_i_i_MenuAdvancedFilterNotActive];
        }
        else if (hasFilterOptions)
        {
            return optionApplied ? CssClassAndStyle[CssClass.Column_Options_i_i_MenuFiltreActive] :
                CssClassAndStyle[CssClass.Column_Options_i_i_MenuFiltreNotActive];
        }
        else
        {
            return "";
        }
    }

    /// <summary>
    /// Resolves the CSS style for column options.
    /// </summary>
    private string GetColumnOptionCssStyle()
    {
        if (ColumnOptions != null)
        {
            return optionApplied ? CssClassAndStyle[CssStyle.Column_Options_i_i_ColumnOptionActive] :
                CssClassAndStyle[CssStyle.Column_Options_i_i_ColumnOptionNotActive];
        }
        else if (hasAdvancedFilterOptions)
        {
            return optionApplied ? CssClassAndStyle[CssStyle.Column_Options_i_i_MenuAdvancedFilterActive] :
                CssClassAndStyle[CssStyle.Column_Options_i_i_MenuAdvancedFilterNotActive];
        }
        else if (hasFilterOptions)
        {
            return optionApplied ? CssClassAndStyle[CssStyle.Column_Options_i_i_MenuFiltreActive] :
                CssClassAndStyle[CssStyle.Column_Options_i_i_MenuFiltreNotActive];
        }
        else
        {
            return "";
        }
    }

    /// <summary>
    /// Gets the CSS class corresponding to the sort direction of this column.
    /// Uses the dictionary of sort direction CSS classes to associate each sort direction with its corresponding CSS class.
    /// </summary>
    private string GetSortClass()
    {
        return Grid.GetSortCssClass(_lastAssignedColumn!);
    }

    /// <summary>
    /// Gets the CSS style corresponding to the sort direction of this column.
    /// Uses the dictionary of sort direction CSS classes to associate each sort direction with its corresponding CSS class.
    /// </summary>
    private string GetSortStyle()
    {
        return Grid.GetSortCssStyle(_lastAssignedColumn!);
    }
}
