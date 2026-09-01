// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components.Server.Circuits;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;

namespace Microsoft.AspNetCore.Components.Hosting;

internal sealed class NavigationManagerJSRuntimeInitializer(
    IServiceProvider services,
    InteractiveServerContext context) : IServerHostInitializer
{
    public int Order => -150;

    public bool RequiresJSInterop => true;

    public Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (!context.IsInteractive)
        {
            return Task.CompletedTask;
        }

        var navigationManager = services.GetRequiredService<NavigationManager>();
        var jsRuntime = services.GetRequiredService<IJSRuntime>();
        ((RemoteNavigationManager)navigationManager).AttachJsRuntime(jsRuntime);

        return Task.CompletedTask;
    }
}
