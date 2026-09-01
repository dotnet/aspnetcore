// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components.Endpoints;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Components.Hosting;

internal sealed class NavigationManagerInitializer(
    IServiceProvider services,
    IHostStartupValues startupValues) : IHostInitializer
{
    public int Order => -200;

    public Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var httpContextStartupValues =
            services.GetRequiredKeyedService<HttpContextHostStartupValues>(typeof(IHostStartupValues));
        if (!ReferenceEquals(startupValues, httpContextStartupValues))
        {
            return Task.CompletedTask;
        }

        var navigationManager = services.GetRequiredService<NavigationManager>();
        var renderer = services.GetRequiredService<EndpointHtmlRenderer>();
        ((IHostEnvironmentNavigationManager)navigationManager).Initialize(
            startupValues.GetRequired(NavigationHttpContextStartupValueProvider.BaseUriKey),
            startupValues.GetRequired(NavigationHttpContextStartupValueProvider.LocationHrefKey),
            renderer.HandleNavigationAsync);

        return Task.CompletedTask;
    }
}
