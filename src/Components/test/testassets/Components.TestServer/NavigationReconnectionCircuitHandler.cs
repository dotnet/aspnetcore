// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components.Server.Circuits;

namespace TestServer;

public class NavigationReconnectionCircuitHandler : CircuitHandler
{
    private readonly TaskCompletionSource _disconnected = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private Circuit? _circuit;

    public string CircuitId => _circuit?.Id ?? throw new InvalidOperationException("The circuit has not been opened.");

    public int ConnectionUpCount { get; private set; }

    public Task Disconnected => _disconnected.Task;

    public override Task OnCircuitOpenedAsync(Circuit circuit, CancellationToken cancellationToken)
    {
        _circuit = circuit;
        return Task.CompletedTask;
    }

    public override Task OnConnectionUpAsync(Circuit circuit, CancellationToken cancellationToken)
    {
        ConnectionUpCount++;
        return Task.CompletedTask;
    }

    public override Task OnConnectionDownAsync(Circuit circuit, CancellationToken cancellationToken)
    {
        _disconnected.TrySetResult();
        return Task.CompletedTask;
    }
}
