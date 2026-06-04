// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection;

namespace TestServer;

/// <summary>
/// Startup used by <c>QuickGridInteractiveCompatTest</c>. Mirrors the pattern of
/// <see cref="RazorComponentEndpointsNoInteractivityStartup{TRootComponent}"/>:
/// it is registered as its own host in <c>Program.cs</c> and bound by its own
/// test fixture so the QuickGrid URL-navigation feature switch can be flipped off
/// for this scenario without races against other in-process fixtures that share
/// the same <see cref="AppContext"/> and <c>QuickGridFeatureFlags</c> type initializer.
/// </summary>
public class RazorComponentEndpointsCompatStartup<TRootComponent>
    : RazorComponentEndpointsStartup<TRootComponent>
{
    private const string EnableUrlBasedNavigationSwitchName =
        "Microsoft.AspNetCore.Components.QuickGrid.EnableUrlBasedQuickGridNavigationAndSorting";

    public RazorComponentEndpointsCompatStartup(IConfiguration configuration)
        : base(configuration)
    {
        // base ctor already reset the switch to 'enabled' (its default). Override that to
        // 'disabled' so the compat scenario renders the legacy button-based QuickGrid UI.
        ResetQuickGridUrlNavigationSwitch(enabled: false);
    }
}
