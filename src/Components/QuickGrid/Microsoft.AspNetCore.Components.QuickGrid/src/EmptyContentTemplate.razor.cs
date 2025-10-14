// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components.QuickGrid.Infrastructure;

namespace Microsoft.AspNetCore.Components.QuickGrid;

/// <summary>
/// A component that is displayed when the <see cref="QuickGrid{TGridItem}"/> has no data available.
/// </summary>
/// <typeparam name="TGridItem">The type of data represented by each row in the grid.</typeparam>
public partial class EmptyContentTemplate<TGridItem>
{
    [CascadingParameter] internal InternalGridContext<TGridItem> InternalGridContext { get; set; } = default!;

    /// <summary>
    /// Defines the content to be rendered by <see cref="QuickGrid{TGridItem}"/> when no data is available.
    /// </summary>
    [Parameter] public RenderFragment? ChildContent { get; set; }
}
