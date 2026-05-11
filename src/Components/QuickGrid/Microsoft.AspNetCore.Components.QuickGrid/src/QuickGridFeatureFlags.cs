// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components.QuickGrid;

internal static class QuickGridFeatureFlags
{
    internal static bool EnableUrlBasedQuickGridNavigationAndSorting =>
        !AppContext.TryGetSwitch("Microsoft.AspNetCore.Components.QuickGrid.EnableUrlBasedQuickGridNavigationAndSorting", out var isEnabled) || isEnabled;
}
