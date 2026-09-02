// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components.Hosting;
using Microsoft.Extensions.DependencyInjection;

namespace Components.TestServer.RazorComponents;

public sealed class HostInitializationState
{
    public List<string> Events { get; } = [];
    public int ClickCount { get; set; }
}

internal sealed class TestBrowserStartupValueProvider : IBrowserStartupValueProvider
{
    public IReadOnlyList<string> Keys { get; } = ["navigator.language"];
}

internal sealed class StartupValuesHostInitializer : IHostInitializer
{
    public int Order => -100;

    public Task InitializeAsync(IServiceProvider services, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var startupValues = services.GetRequiredService<IHostStartupValues>();
        var state = services.GetRequiredService<HostInitializationState>();
        state.Events.Add($"values:{startupValues.GetValue("navigator.language") ?? "-"}");

        return Task.CompletedTask;
    }
}

internal sealed class JSReadyHostInitializer : IHostInitializer
{
    public int Order => 100;
    public bool RequiresJSInterop => true;

    public Task InitializeAsync(IServiceProvider services, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var state = services.GetRequiredService<HostInitializationState>();
        state.Events.Add("js-ready");

        return Task.CompletedTask;
    }
}
