// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components.Server.Circuits;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Components.Hosting;

internal sealed class NavigationManagerInitializer : IHostInitializer
{
    public int Order => -200;

    public Task InitializeAsync(IServiceProvider services, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var context = services.GetRequiredService<InteractiveServerContext>();
        if (!context.IsInteractive)
        {
            return Task.CompletedTask;
        }

        var startupValues = services.GetRequiredService<IHostStartupValues>();
        var navigationManager = services.GetRequiredService<NavigationManager>();
        ((RemoteNavigationManager)navigationManager).Initialize(
            startupValues.GetRequired(NavigationBrowserStartupValueProvider.BaseUriKey),
            startupValues.GetRequired(NavigationBrowserStartupValueProvider.LocationHrefKey));

        return Task.CompletedTask;
    }
}
