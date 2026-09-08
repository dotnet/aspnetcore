// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.AspNetCore.Components.WebAssembly.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Components.Hosting;

internal sealed class NavigationManagerInitializer : IHostInitializer
{
    public int Order => int.MinValue;

    public Task InitializeHostAsync(IServiceProvider services, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var startupValues = services.GetRequiredService<IHostStartupValues>();
        var baseUri = startupValues.GetRequired(NavigationBrowserStartupValueProvider.BaseUriKey);
        var baseAddress = WebAssemblyNavigationManager.NormalizeBaseUriForHostEnvironment(baseUri);
        var hostEnvironment = services.GetRequiredService<IWebAssemblyHostEnvironment>();
        if (!string.Equals(baseAddress, hostEnvironment.BaseAddress, StringComparison.Ordinal))
        {
            throw new InvalidOperationException("The browser base URI changed during host initialization.");
        }

        var navigationManager = services.GetRequiredService<WebAssemblyNavigationManager>();
        navigationManager.InitializeNavigation(
            baseUri,
            startupValues.GetRequired(NavigationBrowserStartupValueProvider.LocationHrefKey));

        return Task.CompletedTask;
    }
}
