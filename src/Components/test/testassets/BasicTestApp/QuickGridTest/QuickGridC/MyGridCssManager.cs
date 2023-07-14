// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components.QuickGrid.QuickGridCollection;

namespace BasicTestApp.QuickGridTest.QuickGridC;

public class MyGridCssManager : GridHtmlCssManager
{
    protected override Dictionary<CssClass, string> InitializedClassCss()
    {
        return new()
        {
            { CssClass.Grid_div, "" },
            { CssClass.Grid_div_table, "table table-dark table-hover" },
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
}
