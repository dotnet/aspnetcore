// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components.QuickGrid.QuickGridCollection;

/// <summary>
/// Class for managing CSS classes and styles of HTML elements in the <see cref="QuickGridC{TGridItem}"/> grid.
/// Contains dictionaries associating each HTML element with its CSS class or style.
/// </summary>
public class GridHtmlCssManager
{
    private readonly Dictionary<CssClass, string> classHtmls;
    private readonly Dictionary<CssStyle, string> styleCsss;
    /// <summary>
    /// 
    /// </summary>
    public GridHtmlCssManager()
    {
        classHtmls = InitializedClassCss();
        styleCsss = InitializedStyleCss();
    }
    /// <summary>
    /// 
    /// </summary>
    /// <param name="classHtml"></param>
    /// <returns></returns>
    public string this[CssClass classHtml] { get => classHtmls[classHtml]; set => classHtmls[classHtml] = value; }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="styleCss"></param>
    /// <returns></returns>
    public string this[CssStyle styleCss] { get => styleCsss[styleCss]; set => styleCsss[styleCss] = value; }

    /// <summary xml:lang="fr">
    /// Initialized
    /// </summary>
    protected virtual Dictionary<CssClass, string> InitializedClassCss()
    {
        return new()
        {
            { CssClass.Grid_div, "" },
            { CssClass.Grid_div_table, "table" },
            { CssClass.Grid_div_table_thead, "" },
            { CssClass.Grid_div_table_thead_tr, "" },
            { CssClass.Grid_div_table_thead_tr_th, "" },
            { CssClass.Grid_div_table_tbody, "" },
            { CssClass.Grid_div_table_tbody_tr, "" },
            { CssClass.Grid_div_table_tbody_tr_td, "" },
            { CssClass.Column_Sort_i, "" },
            { CssClass.Column_Sort_i_i_SortAsc, "sorting_ascending" },
            { CssClass.Column_Sort_i_i_SortDesc, "sorting_descending" },
            { CssClass.Column_Sort_i_i_Sortdefault, "sorting_default" },
            { CssClass.Column_Sort_i_i_SortNot, "" },
            { CssClass.Column_Options_i, "dropdown" },
            { CssClass.Column_Options_i_div_NoShow, "dropdown-content" },
            { CssClass.Column_Options_i_div_Show, "dropdown-content show" },
            { CssClass.Column_Options_i_i_ColumnOptionActive, "grid-option-Acteved" },
            { CssClass.Column_Options_i_i_ColumnOptionNotActive, "grid-option" },
            { CssClass.Column_Options_i_i_MenuFiltreActive, "grid-filter-Actived" },
            { CssClass.Column_Options_i_i_MenuFiltreNotActive, "grid-filter" },
            { CssClass.Column_Options_i_i_MenuAdvancedFilterActive, "grid-filter-advanced-Active" },
            { CssClass.Column_Options_i_i_MenuAdvancedFilterNotActive, "grid-filter-advanced" },
            { CssClass.MenuFiltre_from, "" },
            { CssClass.MenuFiltre_from_divInput, "form-control" },
            { CssClass.MenuFiltre_from_divInput_selectInputOption, "form-control" },
            { CssClass.MenuFiltre_from_divInput_selectInputEnumValue, "form-control" },
            { CssClass.MenuFiltre_from_divInput_selectInputBoolValue, "form-control" },
            { CssClass.MenuFiltre_from_divInput_inputInputValue, "form-control" },
            { CssClass.MenuFiltre_from_divAction, "form-control d-grid gap-2 d-md-block" },
            { CssClass.MenuFiltre_from_divAction_buttonOk, "btn btn-primary" },
            { CssClass.MenuFiltre_from_divAction_buttonReset, "btn btn-primary" },
            { CssClass.MenuAdvancedFilter_div, "d-grid gap-2 d-md-flex justify-content-between" },
            { CssClass.MenuAdvancedFilter_div_button_Operator, "btn btn-primary btn-sm" },
            { CssClass.MenuAdvancedFilter_div_button_Add, "btn btn-primary btn-sm" },

        };
    }

    /// <summary xml:lang="fr">
    /// Initialized
    /// </summary>
    protected virtual Dictionary<CssStyle, string> InitializedStyleCss()
    {
        return new()
        {
            { CssStyle.Grid_div, "" },
            { CssStyle.Grid_div_table, "" },
            { CssStyle.Grid_div_table_thead, "" },
            { CssStyle.Grid_div_table_thead_tr, "" },
            { CssStyle.Grid_div_table_thead_tr_th, "" },
            { CssStyle.Grid_div_table_tbody, "" },
            { CssStyle.Grid_div_table_tbody_tr, "" },
            { CssStyle.Grid_div_table_tbody_tr_td, "" },
            { CssStyle.Column_Sort_i, "float:right" },
            { CssStyle.Column_Sort_i_i_SortAsc, "" },
            { CssStyle.Column_Sort_i_i_SortDesc, "" },
            { CssStyle.Column_Sort_i_i_Sortdefault, "" },
            { CssStyle.Column_Sort_i_i_SortNot, "" },
            { CssStyle.Column_Options_i, "" },
            { CssStyle.Column_Options_i_div_NoShow, "" },
            { CssStyle.Column_Options_i_div_Show, "" },
            { CssStyle.Column_Options_i_i_ColumnOptionActive, "" },
            { CssStyle.Column_Options_i_i_ColumnOptionNotActive, "" },
            { CssStyle.Column_Options_i_i_MenuFiltreActive, "" },
            { CssStyle.Column_Options_i_i_MenuFiltreNotActive, "" },
            { CssStyle.Column_Options_i_i_MenuAdvancedFilterActive, "" },
            { CssStyle.Column_Options_i_i_MenuAdvancedFilterNotActive, "" },
            { CssStyle.MenuFiltre_from, "" },
            { CssStyle.MenuFiltre_from_divInput, "width: max-content;" },
            { CssStyle.MenuFiltre_from_divInput_selectInputOption, "" },
            { CssStyle.MenuFiltre_from_divInput_selectInputEnumValue, "" },
            { CssStyle.MenuFiltre_from_divInput_selectInputBoolValue, "" },
            { CssStyle.MenuFiltre_from_divInput_inputInputValue, "" },
            { CssStyle.MenuFiltre_from_divAction, "" },
            { CssStyle.MenuFiltre_from_divAction_buttonOk, "" },
            { CssStyle.MenuFiltre_from_divAction_buttonReset, "" },
            { CssStyle.MenuAdvancedFilter_div, "" },
            { CssStyle.MenuAdvancedFilter_div_button_Operator, "" },
            { CssStyle.MenuAdvancedFilter_div_button_Add, "" },
        };
    }
}
