// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;

namespace Microsoft.AspNetCore.Components.QuickGrid;

internal static class QuickGridFeatureFlags
{
    private const string EnableUrlBasedNavigationSwitchName =
        "Microsoft.AspNetCore.Components.QuickGrid.EnableUrlBasedQuickGridNavigationAndSorting";

#pragma warning disable IDE0044
    private static bool s_enableUrlBasedQuickGridNavigationAndSorting =
        !AppContext.TryGetSwitch(EnableUrlBasedNavigationSwitchName, out var isEnabled) || isEnabled;
#pragma warning restore IDE0044

    [FeatureSwitchDefinition(EnableUrlBasedNavigationSwitchName)]
    internal static bool EnableUrlBasedQuickGridNavigationAndSorting => s_enableUrlBasedQuickGridNavigationAndSorting;
}
