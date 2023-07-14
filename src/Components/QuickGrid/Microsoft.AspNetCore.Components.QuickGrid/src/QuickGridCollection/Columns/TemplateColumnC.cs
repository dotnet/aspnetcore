// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components.QuickGrid.QuickGridCollection.Columns;
using Microsoft.AspNetCore.Components.Rendering;

namespace Microsoft.AspNetCore.Components.QuickGrid.QuickGridCollection;

/// <summary>
/// 
/// </summary>
/// <typeparam name="TGridItem"></typeparam>
public class TemplateColumnC<TGridItem> : ColumnCBase<TGridItem>
{
    private static readonly RenderFragment<TGridItem> EmptyChildContent = _ => builder => { };
    /// <summary>
    /// 
    /// </summary>
    [Parameter] public RenderFragment<TGridItem> ChildContent { get; set; } = EmptyChildContent;

    /// <inheritdoc/>
    protected override void OnParametersSet()
    {
        var IsNewProperty = InternalGridContext.Grid.IsFirstRender;
        if (IsNewProperty)
        {
            AddColumn();
        }
    }

    /// <inheritdoc/>
    protected internal override void CellContent(RenderTreeBuilder builder, TGridItem item)
    {
        builder.AddContent(0, ChildContent(item));
    }
}
