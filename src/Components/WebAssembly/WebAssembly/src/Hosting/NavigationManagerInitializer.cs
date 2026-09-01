// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components.WebAssembly.Services;

namespace Microsoft.AspNetCore.Components.Hosting;

internal sealed class NavigationManagerInitializer(
    IHostStartupValues startupValues,
    NavigationManager navigationManager) : IHostInitializer
{
    public int Order => -200;

    public Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        ((WebAssemblyNavigationManager)navigationManager).InitializeNavigation(
            startupValues.GetRequired(NavigationBrowserStartupValueProvider.BaseUriKey),
            startupValues.GetRequired(NavigationBrowserStartupValueProvider.LocationHrefKey));

        return Task.CompletedTask;
    }
}
