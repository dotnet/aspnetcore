// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components.Server.Circuits;

namespace TestServer;

internal sealed class CircuitHandlerTestGate : CircuitHandler
{
    private readonly object _lock = new();
    private readonly Dictionary<string, GateState> _states = [];
    private string? _armedToken;

    public bool Arm(string token)
    {
        lock (_lock)
        {
            if (_states.Count > 0)
            {
                return false;
            }

            _states[token] = new();
            _armedToken = token;
            return true;
        }
    }

    public bool IsEntered(string token)
    {
        lock (_lock)
        {
            return _states.TryGetValue(token, out var state) && state.Entered.Task.IsCompleted;
        }
    }

    public bool Release(string token)
    {
        lock (_lock)
        {
            if (!_states.TryGetValue(token, out var state))
            {
                return false;
            }

            if (_armedToken == token)
            {
                _armedToken = null;
            }

            if (state.Armed)
            {
                _states.Remove(token);
            }

            return state.Release.TrySetResult();
        }
    }

    public override async Task OnCircuitOpenedAsync(Circuit circuit, CancellationToken cancellationToken)
    {
        string? token = null;
        GateState? state = null;
        lock (_lock)
        {
            if (_armedToken is not null)
            {
                token = _armedToken;
                state = _states[token];
                _armedToken = null;
                state.Armed = false;
                state.Entered.SetResult();
            }
        }

        if (state is not null)
        {
            try
            {
                await state.Release.Task.WaitAsync(TimeSpan.FromSeconds(30));
            }
            finally
            {
                lock (_lock)
                {
                    _states.Remove(token);
                }
            }
        }
    }

    private sealed class GateState
    {
        public bool Armed { get; set; } = true;

        public TaskCompletionSource Entered { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public TaskCompletionSource Release { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
    }
}

internal static class CircuitHandlerTestEndpoints
{
    public static void MapCircuitHandlerTestEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/circuit-handler-test/arm/{token}", (string token, CircuitHandlerTestGate gate) =>
            gate.Arm(token) ? Results.Ok() : Results.Conflict());

        endpoints.MapGet("/circuit-handler-test/entered/{token}", (string token, CircuitHandlerTestGate gate) =>
            Results.Json(new { entered = gate.IsEntered(token) }));

        endpoints.MapPost("/circuit-handler-test/release/{token}", (string token, CircuitHandlerTestGate gate) =>
            gate.Release(token) ? Results.Ok() : Results.NotFound());
    }
}