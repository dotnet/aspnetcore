// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components.QuickGrid.QuickGridCollection.Infrastructure;

internal sealed class InternalGridContext<TGridItem>(QuickGridC<TGridItem> grid)
{
    public QuickGridC<TGridItem> Grid { get; } = grid;
}
