// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components.Routing;
using Microsoft.AspNetCore.Components.Server.Circuits;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;

namespace Microsoft.AspNetCore.Components.Hosting;

internal sealed class NavigationServicesJSRuntimeInitializer : IHostInitializer
{
    public int Order => -100;

    public Task InitializeBrowserAsync(IServiceProvider services, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var context = services.GetRequiredService<InteractiveServerContext>();
        if (!context.IsInteractive)
        {
            return Task.CompletedTask;
        }

        var navigationInterception = services.GetRequiredService<INavigationInterception>();
        var scrollToLocationHash = services.GetRequiredService<IScrollToLocationHash>();
        var jsRuntime = services.GetRequiredService<IJSRuntime>();
        ((RemoteNavigationInterception)navigationInterception).AttachJSRuntime(jsRuntime);
        ((RemoteScrollToLocationHash)scrollToLocationHash).AttachJSRuntime(jsRuntime);

        return Task.CompletedTask;
    }
}
