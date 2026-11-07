// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components.Server.Circuits;

namespace TestServer;

public sealed class CircuitPersistenceTestGate : CircuitHandler
{
    private TaskCompletionSource _circuitClosed = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private TaskCompletionSource _connectionDown = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private string _currentCircuitId = string.Empty;
    private string _expectedCircuitId = string.Empty;

    public Task CircuitClosed => _circuitClosed.Task;

    public Task ConnectionDown => _connectionDown.Task;

    public void Reset()
    {
        _expectedCircuitId = _currentCircuitId;
        _circuitClosed = new(TaskCreationOptions.RunContinuationsAsynchronously);
        _connectionDown = new(TaskCreationOptions.RunContinuationsAsynchronously);
    }

    public override Task OnCircuitOpenedAsync(Circuit circuit, CancellationToken cancellationToken)
    {
        _currentCircuitId = circuit.Id;
        return Task.CompletedTask;
    }

    public override Task OnCircuitClosedAsync(Circuit circuit, CancellationToken cancellationToken)
    {
        // Eviction persists the disconnected circuit before closing it.
        if (circuit.Id == _expectedCircuitId)
        {
            _circuitClosed.TrySetResult();
        }

        return Task.CompletedTask;
    }

    public override Task OnConnectionDownAsync(Circuit circuit, CancellationToken cancellationToken)
    {
        if (circuit.Id == _expectedCircuitId)
        {
            _connectionDown.TrySetResult();
        }

        return Task.CompletedTask;
    }
}
