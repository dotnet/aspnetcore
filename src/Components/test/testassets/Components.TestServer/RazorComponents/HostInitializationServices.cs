// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components.Hosting;

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

internal sealed class StartupValuesHostInitializer(
    IHostStartupValues startupValues,
    HostInitializationState state) : IHostInitializer
{
    public int Order => -100;

    public Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        state.Events.Add($"values:{startupValues.GetValue("navigator.language") ?? "-"}");

        return Task.CompletedTask;
    }
}

internal sealed class JSReadyHostInitializer(HostInitializationState state) : IHostInitializer
{
    public int Order => 100;
    public bool RequiresJSInterop => true;

    public Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        state.Events.Add("js-ready");

        return Task.CompletedTask;
    }
}
