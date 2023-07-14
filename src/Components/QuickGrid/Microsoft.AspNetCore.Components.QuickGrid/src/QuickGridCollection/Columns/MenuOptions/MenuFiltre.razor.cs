// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components.QuickGrid.QuickGridCollection.Columns.MenuOptions;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;

namespace Microsoft.AspNetCore.Components.QuickGrid.QuickGridCollection.Columns;

/// <summary>
/// 
/// </summary>
/// <typeparam name="TGridItem"></typeparam>
public partial class MenuFiltre<TGridItem> : ComponentBase
{
    /// <summary>
    /// This string contains the HTML input type to use for the search form input fields.
    /// It is determined based on the type of property associated with the input field, specified by the <see cref="TypeOfProperty"/> property.
    /// For example, for string type properties, it takes the value "text", for numeric type properties, it takes the value "number", etc.
    /// </summary>    
    private string htmlInputType = string.Empty;

    /// <summary>
    /// Gets or sets a value indicating whether the filter is applied.
    /// This property is used to prevent triggering of the <see cref="QuickGridC{TGridItem}.FilterSortChanged"/> event in the <see cref="ResetColumnFilters"/> method if the <see cref="ApplyFilters"/> method has not been called previously.
    /// </summary>    
    private bool filterApplied;

    /// <summary>
    /// This field is used by the <see cref="MenuAdvancedFilter{TGridItem}"/> class to indicate the number of filters added in the column.
    /// </summary>    
    protected int columnFilterAdditions;

    /// <summary>
    /// This list contains the objects that correspond to the search form input fields.
    /// It is used by the <see cref="MenuAdvancedFilter{TGridItem}"/> class to manage the number of filter additions in the column.
    /// </summary>
    protected List<object?> filterValues = new() { null };

    /// <summary>
    /// This list contains the filter options in the search form.
    /// The first list manages the number of filter additions in the column, while the second list allows you to choose the type of search to perform.
    /// </summary>
    protected List<List<Enum>> filterOptions = default!;

    /// <summary>
    /// This list contains the selected filter options for enumeration type fields in the search form.
    /// The list manages the number of filter additions in the column
    /// </summary>
    protected List<Enum> selectedFilterOptions = default!;

    /// <summary>
    /// The default value of <see cref="selectedFilterOptions"/>.
    /// </summary>
    protected List<Enum> selectedFilterOptionsDefault = default!;

    /// <summary>
    /// The Enum Type to use for search options.
    /// </summary>
    protected Type optionsType = default!;

    /// <summary>
    /// This list contains lambda expressions generated from selected filter options in the search form.
    /// Expressions are created using values contained in <see cref="filterValues"/> and selected filter options.
    /// The list is used by the <see cref="MenuAdvancedFilter{TGridItem}"/> class to manage the number of filter additions in the column.
    /// </summary>
    protected List<Expression<Func<TGridItem, bool>>>? columnFilterExpressions;

    /// <summary>
    /// 
    /// </summary>
    [CascadingParameter] public ColumnCBase<TGridItem> Column { get; set; } = default!;

    /// <summary>
    /// 
    /// </summary>
    protected RenderFragment RenderFragment => FromRender;

    /// <summary>
    /// An instance of the <see cref="QuickGridC{TGridItem}"/> grid.
    /// </summary>
    protected QuickGridC<TGridItem> Grid => Column.Grid;

    /// <summary>
    /// Type of the column property.
    /// </summary>    
    protected Type TypeOfProperty => Column.TypeOfProperty is not null ? Nullable.GetUnderlyingType(Column.TypeOfProperty) ?? Column.TypeOfProperty : throw new MenuOptionException("Column.TypeOfProperty is null");

    /// <summary>
    /// Property expression for the column.
    /// </summary>
    protected Expression<Func<TGridItem, object?>> PropertyExpression => Column.PropertyExpression ?? throw new MenuOptionException("Column.PropertyExpression is null");

    /// <summary>
    /// Object for managing CSS classes and styles of HTML elements in the grid.
    /// </summary>
    protected GridHtmlCssManager CssClassAndStyle => Column.CssClassAndStyle;

    /// <summary>
    /// Gets or sets a value indicating whether the filter is applied.
    /// This property is used to prevent triggering of the <see cref="QuickGridC{TGridItem}.FilterSortChanged"/> event in the <see cref="ResetColumnFilters"/> method if the <see cref="ApplyFilters"/> method has not been called previously.
    /// </summary>
    private bool FilterApplied { get => filterApplied; set => Column.OptionApplied = filterApplied = value; }

    /// <summary>
    /// 
    /// </summary>
    /// <exception cref="NotSupportedException"></exception>
    protected override void OnParametersSet()
    {
        (optionsType, selectedFilterOptions, htmlInputType) = TypeOfProperty switch
        {
            Type t when t == typeof(string) =>
                        (typeof(StringFilterOptions), new List<Enum>() { StringFilterOptions.Contains }, "text"),
            Type t when t == typeof(DateTime) || t == typeof(DateTimeOffset) =>
                        (typeof(DataFilterOptions), new() { DataFilterOptions.Equal }, "datetime-local"),
            Type t when t == typeof(DateOnly) =>
                        (typeof(DataFilterOptions), new() { DataFilterOptions.Equal }, "date"),
            Type t when t == typeof(TimeOnly) || t == typeof(TimeSpan) =>
                        (typeof(DataFilterOptions), new() { DataFilterOptions.Equal }, "time"),
            Type t when t == typeof(bool) =>
                        (typeof(BoolFilterOptions), new() { DataFilterOptions.Equal }, "select"),
            Type t when IsNumber(t) =>
                        (typeof(DataFilterOptions), new() { DataFilterOptions.Equal }, "number"),
            Type t when t.IsEnum =>
                        (typeof(EnumFilterOptions), new() { EnumFilterOptions.Equal }, string.Empty),
            _ => throw new NotSupportedException($"type {TypeOfProperty} not supported")
        };
        selectedFilterOptionsDefault = selectedFilterOptions.ToList();
    }
    private static bool IsNumber(Type value)
    {
        return value == typeof(sbyte)
            || value == typeof(byte)
            || value == typeof(short)
            || value == typeof(ushort)
            || value == typeof(int)
            || value == typeof(uint)
            || value == typeof(long)
            || value == typeof(ulong)
            || value == typeof(float)
            || value == typeof(double)
            || value == typeof(decimal);
    }

    /// <summary>
    /// This method is called when the user clicks on the "OK" button of the search form.
    /// It creates lambda expressions from selected filter options in the form and adds them to the list of column filter expressions.
    /// Expressions are created using values contained in <see cref="filterValues"/> and selected filter options.
    /// If valid expressions are created, they are added to the <see cref="columnFilterExpressions"/> list and the filter is applied by calling the <see cref="ApplyColumnFilterFromGrid"/> method.
    /// </summary>
    private void ApplyFilters()
    {
        for (var i = 0; i < columnFilterAdditions + 1; i++)
        {
            Func<Expression<Func<TGridItem, bool>>> action;

            if (selectedFilterOptions[i] is StringFilterOptions optionFiltreString)
            {
                action = optionFiltreString switch
                {
                    StringFilterOptions.Contains => new(() => CreateStringFilterExpression("Contains", filterValues[i])),
                    StringFilterOptions.StartsWith => new(() => CreateStringFilterExpression("StartsWith", filterValues[i])),
                    StringFilterOptions.EndsWith => new(() => CreateStringFilterExpression("EndsWith", filterValues[i])),
                    StringFilterOptions.Equal => new(() => CreateDataFilterExpression(ExpressionType.Equal, filterValues[i])),
                    StringFilterOptions.NotEqual => new(() => CreateDataFilterExpression(ExpressionType.NotEqual, filterValues[i])),
                    _ => throw new NotSupportedException()
                };
            }
            else if (selectedFilterOptions[i] is DataFilterOptions optionFiltreData)
            {
                action = optionFiltreData switch
                {
                    DataFilterOptions.Equal => new(() => CreateDataFilterExpression(ExpressionType.Equal, filterValues[i])),
                    DataFilterOptions.GreaterThan => new(() => CreateDataFilterExpression(ExpressionType.GreaterThan, filterValues[i])),
                    DataFilterOptions.GreaterThanOrEqual => new(() => CreateDataFilterExpression(ExpressionType.GreaterThanOrEqual, filterValues[i])),
                    DataFilterOptions.LessThan => new(() => CreateDataFilterExpression(ExpressionType.LessThan, filterValues[i])),
                    DataFilterOptions.LessThanOrEqual => new(() => CreateDataFilterExpression(ExpressionType.LessThanOrEqual, filterValues[i])),
                    DataFilterOptions.NotEqual => new(() => CreateDataFilterExpression(ExpressionType.NotEqual, filterValues[i])),
                    _ => throw new NotSupportedException()
                };
            }
            else if (selectedFilterOptions[i] is EnumFilterOptions optionFiltreEnum)
            {
                action = optionFiltreEnum switch
                {
                    EnumFilterOptions.Equal => new(() => CreateDataFilterExpression(ExpressionType.Equal, filterValues[i])),
                    EnumFilterOptions.NotEqual => new(() => CreateDataFilterExpression(ExpressionType.NotEqual, filterValues[i])),
                    _ => throw new NotSupportedException()
                };
            }
            else
            {
                throw new NotSupportedException();
            }

            var lambda = action.Invoke();

            if (columnFilterExpressions == null)
            {
                columnFilterExpressions = new() { lambda };
            }
            else
            {
                if (columnFilterExpressions.Count < columnFilterAdditions + 1)
                {
                    columnFilterExpressions.Add(lambda);
                }
                else
                {
                    columnFilterExpressions[i] = lambda;
                }
            }

        }
        if (columnFilterExpressions != null && columnFilterExpressions.Count != 0)
        {
            ApplyColumnFilterFromGrid();
            FilterApplied = true;
            Column.IsOptionVisible = false;
        }
    }

    /// <summary>
    /// This method adds the column filter to the grid.
    /// </summary>
    protected virtual void ApplyColumnFilterFromGrid()
    {
        if (columnFilterExpressions != null && columnFilterExpressions.Count != 0)
        {
            Grid.ApplyColumnFilter(columnFilterExpressions.First(), Column);
        }
    }

    /// <summary>
    /// This method resets selected filter options in the search form and removes column filter expressions.
    /// If a filter has been applied previously, it is removed from the grid <see cref="QuickGridC{TGridItem}.columnFilters"/>.
    /// </summary>
    protected void ResetColumnFilters()
    {
        filterValues = new() { null };
        selectedFilterOptions = selectedFilterOptionsDefault.ToList();
        filterOptions = default!;
        columnFilterExpressions = null!;
        columnFilterAdditions = 0;
        if (FilterApplied)
        {
            Grid.ApplyColumnFilter(null, Column);
        }

        FilterApplied = false;
    }

    /// <summary>
    /// This method creates a lambda expression for string type fields using the specified method and provided value.
    /// </summary>
    /// <param name="methode">The method to use to create the lambda expression.</param>
    /// <param name="objValue">The value to use in the lambda expression.</param>
    /// <returns>A lambda expression representing the filter for a string type field.</returns>
    [UnconditionalSuppressMessage("Trimming", "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code", Justification = "<Pending>")]
    private Expression<Func<TGridItem, bool>> CreateStringFilterExpression(string methode, object? objValue)
    {
        MemberExpression memberExp = null!;
        if (objValue == null)
        {
            return CreateDataFilterExpression(ExpressionType.Equal, objValue);
        }

        if (PropertyExpression.Body is MemberExpression memberExpression)
        {
            memberExp = memberExpression;
        }
        else if (PropertyExpression.Body is UnaryExpression unaryExp && unaryExp.Operand is MemberExpression memberExpression1)
        {
            memberExp = memberExpression1;
        }
        if (memberExp != null)
        {
            var parameter = Expression.Parameter(typeof(TGridItem), "x");
            var property = Expression.Property(parameter, memberExp.Member.Name);

            var methodCallExpression = Expression.Call(
                property,
                methode,
                Type.EmptyTypes,
                Expression.Constant(objValue)
            );

            return Expression.Lambda<Func<TGridItem, bool>>(methodCallExpression, parameter);
        }
        else
        {
            throw new MenuOptionException("Not MemberExpression");
        }
    }

    /// <summary>
    /// This method creates a lambda expression for numeric, date, enum or bool type fields using the comparison type and provided value.
    /// </summary>
    /// <param name="comparisonType">The comparison type to use to create the lambda expression.</param>
    /// <param name="objValue">The value to use in the lambda expression.</param>
    /// <returns>A lambda expression representing the filter for a numeric, date, enum or bool type field.</returns>
    [UnconditionalSuppressMessage("Trimming", "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code", Justification = "<Pending>")]
    private Expression<Func<TGridItem, bool>> CreateDataFilterExpression(ExpressionType comparisonType, object? objValue)
    {
        MemberExpression memberExp = null!;
        if (PropertyExpression.Body is MemberExpression memberExpression)
        {
            memberExp = memberExpression;
        }
        else if (PropertyExpression.Body is UnaryExpression unaryExp && unaryExp.Operand is MemberExpression memberExpression1)
        {
            memberExp = memberExpression1;
        }

        if (memberExp != null)
        {
            object? objectConverted;
            if (objValue == null)
            {
                objectConverted = null;
            }
            else if (TypeOfProperty.IsEnum)
            {
                objectConverted = Enum.Parse(TypeOfProperty, (string)objValue);
            }
            else if (TypeOfProperty == typeof(DateOnly) || Nullable.GetUnderlyingType(TypeOfProperty) == typeof(DateOnly))
            {
                objectConverted = DateOnly.Parse((string)objValue, CultureInfo.InvariantCulture);
            }
            else if (TypeOfProperty == typeof(DateTimeOffset))
            {
                objectConverted = (DateTimeOffset?)DateTimeOffset.Parse((string)objValue, CultureInfo.InvariantCulture).ToUniversalTime();
            }
            else if (TypeOfProperty == typeof(decimal))
            {
                objectConverted = decimal.Parse((string)objValue, CultureInfo.InvariantCulture);
            }
            else
            {
                objectConverted = Convert.ChangeType(objValue, TypeOfProperty, CultureInfo.InvariantCulture);
            }

            var parameter = Expression.Parameter(typeof(TGridItem), "x");
            var property = Expression.Property(parameter, memberExp.Member.Name);

            var unaryExpression = Expression.Convert(property, TypeOfProperty);
            if (objectConverted != null)
            {
                var constant = Expression.Constant(objectConverted);
                var constantConverted = Expression.Convert(constant, TypeOfProperty);
                var comparison = Expression.MakeBinary(comparisonType, unaryExpression, constantConverted);
                return Expression.Lambda<Func<TGridItem, bool>>(comparison, parameter);
            }
            else
            {
                var constant = Expression.Constant(null);

                var salfeComparisonType = comparisonType switch
                {
                    ExpressionType.Equal => ExpressionType.Equal,
                    _ => ExpressionType.NotEqual
                };
                var comparison = Expression.MakeBinary(salfeComparisonType, property, constant);
                return Expression.Lambda<Func<TGridItem, bool>>(comparison, parameter);
            }
        }
        else
        {
            throw new MenuOptionException("Not MemberExpression");
        }

    }

    /// <summary>
    /// Returns the list of filter options for different field types in the search form.
    /// This method is used to generate drop-down menus allowing the user to select filter options in the form.
    /// </summary>
    /// <param name="index">The index of the filter options list to return.</param>
    /// <returns>The list of filter options <see cref="filterOptions"/> at the specified index.</returns>    
    [UnconditionalSuppressMessage("AOT", "IL3050:Calling members annotated with 'RequiresDynamicCodeAttribute' may break functionality when AOT compiling.", Justification = "<Pending>")]
    protected virtual List<Enum> GetListOptionFiltre(int index)
    {
        filterOptions ??= new() { Enum.GetValues(optionsType).Cast<Enum>().ToList() };
        return filterOptions[index];
    }
}
