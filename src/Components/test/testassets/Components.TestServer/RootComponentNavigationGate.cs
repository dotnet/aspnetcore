// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Components.Server.Circuits;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace TestServer;

public sealed class RootComponentNavigationGate : CircuitHandler
{
    private readonly object _sync = new();
    private string? _pendingToken;
    private readonly Dictionary<string, Gate> _gates = [];

    public void Arm(string token)
    {
        lock (_sync)
        {
            if (_pendingToken is not null || !_gates.TryAdd(token, new Gate()))
            {
                throw new InvalidOperationException("A navigation gate is already armed.");
            }

            _pendingToken = token;
        }
    }

    public bool HasEntered(string token)
    {
        lock (_sync)
        {
            return _gates.TryGetValue(token, out var gate) && gate.Entered.Task.IsCompleted;
        }
    }

    public bool Release(string token)
    {
        lock (_sync)
        {
            if (!_gates.Remove(token, out var gate))
            {
                return false;
            }

            if (_pendingToken == token)
            {
                _pendingToken = null;
            }

            gate.Release.TrySetResult();
            return true;
        }
    }

    public override async Task OnCircuitOpenedAsync(Circuit circuit, CancellationToken cancellationToken)
    {
        Gate? gate;
        lock (_sync)
        {
            gate = _pendingToken is not null ? _gates[_pendingToken] : null;
            _pendingToken = null;
            gate?.Entered.TrySetResult();
        }

        if (gate is not null)
        {
            await gate.Release.Task.WaitAsync(cancellationToken);
        }
    }

    private sealed class Gate
    {
        public TaskCompletionSource Entered { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public TaskCompletionSource Release { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
    }
}

public static class RootComponentNavigationGateEndpoints
{
    public static void MapRootComponentNavigationGateEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/navigation-gate/arm/{token}", (string token, RootComponentNavigationGate gate) =>
        {
            gate.Arm(token);
            return Results.Ok();
        });
        endpoints.MapGet("/navigation-gate/entered/{token}", (string token, RootComponentNavigationGate gate) =>
            gate.HasEntered(token) ? Results.Ok() : Results.NotFound());
        endpoints.MapPost("/navigation-gate/release/{token}", (string token, RootComponentNavigationGate gate) =>
            gate.Release(token) ? Results.Ok() : Results.NotFound());
    }
}
