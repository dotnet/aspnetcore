// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components.Routing;

namespace Components.WasmMinimal;

public sealed class StartupNavigationState
{
    public int NotificationCount { get; private set; }
    public string? Location { get; private set; }
    public bool IsNavigationIntercepted { get; private set; }

    public void OnLocationChanged(object? sender, LocationChangedEventArgs args)
    {
        NotificationCount++;
        Location = args.Location;
        IsNavigationIntercepted = args.IsNavigationIntercepted;
    }
}
