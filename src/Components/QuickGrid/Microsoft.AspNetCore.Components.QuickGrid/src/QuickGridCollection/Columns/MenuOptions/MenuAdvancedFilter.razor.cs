// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components.QuickGrid.QuickGridCollection.Columns.MenuOptions;
using Microsoft.AspNetCore.Components.QuickGrid.QuickGridCollection.Infrastructure;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Linq.Expressions;

namespace Microsoft.AspNetCore.Components.QuickGrid.QuickGridCollection.Columns;

/// <summary>
/// 
/// </summary>
/// <typeparam name="TGridItem"></typeparam>
public partial class MenuAdvancedFilter<TGridItem> : MenuFiltre<TGridItem>
{
    /// <summary>
    /// Represents the index of the column filters.
    /// </summary>
    private int filterIndex;

    /// <summary>
    /// Shows or hides the add filter button for each column filter.
    /// </summary>
    private readonly List<bool> showAddFilterButton = new() { false };

    /// <summary>
    /// Operator used to aggregate the column filters <see cref="MenuFiltre{TGridItem}.columnFilterExpressions"/>.
    /// </summary>
    private FilterOperator filterOperator = FilterOperator.AndAlso;

    /// <summary>
    /// Gets the maximum number of filters to apply for this column.
    /// </summary>
    private int MaxColumnFilters => Column.MaxFilters;

    /// <summary>
    /// Adds a filter to the list of column filters.
    /// </summary>
    private void AddColumnFilter()
    {
        if (filterIndex < MaxColumnFilters - 1)
        {
            columnFilterAdditions++;
            if (true)
            {
                showAddFilterButton.Add(true);
            }

            filterValues.Add(string.Empty);
        }
    }

    /// <inheritdoc/>
    [UnconditionalSuppressMessage("AOT", "IL3050:Calling members annotated with 'RequiresDynamicCodeAttribute' may break functionality when AOT compiling.", Justification = "<Pending>")]
    protected override List<Enum> GetListOptionFiltre(int index)
    {
        // Récupère la liste des options de filtre pour le type d'options spécifié
        var enumlist = Enum.GetValues(optionsType).Cast<Enum>().ToList();

        // Initialise la liste des options de filtre si elle est nulle
        filterOptions ??= new() { enumlist };

        // Ajoute une nouvelle liste d'options de filtre si il y a une nouveau filtre
        if (filterOptions.Count < columnFilterAdditions + 1)
        {
            filterOptions.Add(enumlist);
        }

        // Ajoute l'option de filtre sélectionnée par défaut si il y a une nouveau filtre
        if (selectedFilterOptions.Count != columnFilterAdditions + 1)
        {
            var optionfiltervalue = optionsType.Name switch
            {
                nameof(DataFilterOptions) => DataFilterOptions.NotEqual,
                nameof(EnumFilterOptions) => enumlist.FirstOrDefault()!,
                nameof(StringFilterOptions) => StringFilterOptions.Contains,
                _ => throw new NotImplementedException(),
            };
            selectedFilterOptions.Add(optionfiltervalue);
        }

        // Récupère la liste des options de filtre sélectionnées sauf celle actuel
        List<Enum> optionSelecteds;
        optionSelecteds = selectedFilterOptions.ToList();
        optionSelecteds!.RemoveAt(index);

        // Résout la liste des options de filtre en fonction de l'opérateur de filtre et du type d'options
        if (filterOperator == FilterOperator.AndAlso)
        {
            if (optionsType.Name == typeof(DataFilterOptions).Name)
            {
                ResolveDataFilterOptions(enumlist, optionSelecteds);
            }
            else if (optionsType.Name == typeof(EnumFilterOptions).Name)
            {
                ResolveEnumFilterOptions(enumlist, optionSelecteds);
            }
            else if (optionsType.Name == typeof(StringFilterOptions).Name)
            {
                ResolveStringFilterOptions(enumlist, optionSelecteds);
            }
            else
            {
                throw new NotImplementedException();
            }

            // Met à jour l'affichage du bouton d'ajout de filtre en fonction de l'option sélectionnée
            if (selectedFilterOptions[index].ToString() == "Equal")
            {
                showAddFilterButton[index] = false;
            }
            else
            {
                showAddFilterButton[index] = true;
            }
        }
        else
        {
            showAddFilterButton[index] = true;
        }

        // Met à jour et renvoie la liste des options de filtre à l'index spécifié
        return filterOptions[index] = enumlist;
    }

    /// <summary>
    /// Resolves the list of filter options for string type fields based on the selected filter options.
    /// </summary>
    /// <typeparam name="TOption">The type of filter options.</typeparam>
    /// <param name="enumlist">The list of filter options to resolve.</param>
    /// <param name="optionSelecteds">The list of selected filter options.</param>
    private static void ResolveStringFilterOptions<TOption>(List<TOption> enumlist, List<Enum> optionSelecteds)
    {
        if (optionSelecteds.Contains(StringFilterOptions.StartsWith))
        {
            enumlist.RemoveAll(x => x is StringFilterOptions.StartsWith or StringFilterOptions.Equal);
        }

        if (optionSelecteds.Contains(StringFilterOptions.EndsWith))
        {
            enumlist.RemoveAll(x => x is StringFilterOptions.EndsWith or StringFilterOptions.Equal);
        }

        if (optionSelecteds.Any(e => e is StringFilterOptions.Contains or StringFilterOptions.NotEqual))
        {
            enumlist.RemoveAll(x => x is StringFilterOptions.Equal);
        }

        if (optionSelecteds.Contains(StringFilterOptions.Equal))
        {
            enumlist.RemoveAll(x =>
                x is StringFilterOptions.Contains
                or StringFilterOptions.StartsWith
                or StringFilterOptions.EndsWith
                or StringFilterOptions.Equal
                or StringFilterOptions.NotEqual);
        }
    }

    /// <summary>
    /// Resolves the list of filter options for enumeration type fields based on the selected filter options.
    /// </summary>
    /// <typeparam name="TOption">The type of filter options.</typeparam>
    /// <param name="enumlist">The list of filter options to resolve.</param>
    /// <param name="optionSelecteds">The list of selected filter options.</param>
    private static void ResolveEnumFilterOptions<TOption>(List<TOption> enumlist, List<Enum> optionSelecteds)
    {
        if (optionSelecteds.Contains(EnumFilterOptions.Equal))
        {
            enumlist.RemoveAll(x => x is EnumFilterOptions.NotEqual);
        }

        if (optionSelecteds.Contains(EnumFilterOptions.NotEqual))
        {
            enumlist.RemoveAll(x => x is EnumFilterOptions.Equal);
        }
    }

    /// <summary>
    /// Resolves the list of filter options for data type fields based on the selected filter options.
    /// </summary>
    /// <param name="enumlist">The list of filter options to resolve.</param>
    /// <param name="optionSelecteds">The list of selected filter options.</param>
    private static void ResolveDataFilterOptions(List<Enum> enumlist, List<Enum> optionSelecteds)
    {
        if (optionSelecteds.Contains(DataFilterOptions.Equal))
        {
            enumlist.RemoveAll(x =>
            x is DataFilterOptions.NotEqual
            or DataFilterOptions.GreaterThan
            or DataFilterOptions.GreaterThanOrEqual
            or DataFilterOptions.LessThan
            or DataFilterOptions.LessThanOrEqual
            );
        }

        if (optionSelecteds.Contains(DataFilterOptions.GreaterThan))
        {
            enumlist.RemoveAll(x => x is DataFilterOptions.GreaterThanOrEqual
                                    or DataFilterOptions.Equal
                                    or DataFilterOptions.GreaterThan
            );
        }

        if (optionSelecteds.Contains(DataFilterOptions.GreaterThanOrEqual))
        {
            enumlist.RemoveAll(x => x is DataFilterOptions.GreaterThan
                                    or DataFilterOptions.Equal
                                    or DataFilterOptions.GreaterThanOrEqual
            );
        }

        if (optionSelecteds.Contains(DataFilterOptions.LessThan))
        {
            enumlist.RemoveAll(x => x is DataFilterOptions.LessThanOrEqual
                                    or DataFilterOptions.Equal
                                    or DataFilterOptions.LessThan
            );
        }

        if (optionSelecteds.Contains(DataFilterOptions.LessThanOrEqual))
        {
            enumlist.RemoveAll(x => x is DataFilterOptions.LessThan
                                    or DataFilterOptions.Equal
                                    or DataFilterOptions.LessThanOrEqual
            );
        }

        if (optionSelecteds.Contains(DataFilterOptions.NotEqual))
        {
            enumlist.RemoveAll(x => x is DataFilterOptions.Equal);
        }
    }

    /// <inheritdoc/>
    protected override void ApplyColumnFilterFromGrid()
    {
        if (columnFilterExpressions != null)
        {
            var parameter = Expression.Parameter(typeof(TGridItem), "x");
            var replacedExpressions = columnFilterExpressions.Select(e => ((Expression<Func<TGridItem, bool>>)ParameterReplacer.Replace(e, e.Parameters[0], parameter)).Body);
            Expression expression;
            if (filterOperator == FilterOperator.And)
            {
                expression = replacedExpressions.Aggregate(Expression.And);
            }
            else if (filterOperator == FilterOperator.AndAlso)
            {
                expression = replacedExpressions.Aggregate(Expression.AndAlso);
            }
            else if (filterOperator == FilterOperator.AndAssign)
            {
                expression = replacedExpressions.Aggregate(Expression.AndAssign);
            }
            else if (filterOperator == FilterOperator.Or)
            {
                expression = replacedExpressions.Aggregate(Expression.Or);
            }
            else if (filterOperator == FilterOperator.OrElse)
            {
                expression = replacedExpressions.Aggregate(Expression.OrElse);
            }
            else if (filterOperator == FilterOperator.OrElse)
            {
                expression = replacedExpressions.Aggregate(Expression.OrAssign);
            }
            else
            {
                throw new MenuOptionException();
            }

            var lambda = Expression.Lambda<Func<TGridItem, bool>>(expression, parameter);
            if (lambda != null)
            {
                Grid.ApplyColumnFilter(lambda, Column);
            }
        }
    }

    // todo : remplace le bouton par select
    /// <summary>
    /// Toggles between the values of the filter operator to assign the property <see cref="MenuAdvancedFilter{TGridItem}.filterOperator"/>.
    /// </summary>
    /// <exception cref="NotImplementedException">Thrown if the value of the filter operator is not handled.</exception>
    private void ToggleFilterOperator()
    {
        bool reset;
        (filterOperator, reset) = filterOperator switch
        {
            FilterOperator.AndAlso => (FilterOperator.Or, false),
            FilterOperator.Or => (FilterOperator.AndAlso, true),
            _ => throw new NotImplementedException()
        };
        if (reset)
        {
            ResetColumnFilters();
        }
    }
}
