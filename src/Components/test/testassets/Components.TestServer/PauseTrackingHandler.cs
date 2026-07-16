// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;
using Microsoft.AspNetCore.Components.Server.Circuits;

namespace TestServer;

public class PauseTrackingHandler : CircuitHandler
{
    private static readonly ConcurrentDictionary<string, Circuit> s_circuits = new();

    public Circuit? CurrentCircuit { get; private set; }

    public static Circuit? GetCircuit(string id)
        => s_circuits.TryGetValue(id, out var c) ? c : null;

    public override Task OnConnectionUpAsync(Circuit circuit, CancellationToken cancellationToken)
    {
        CurrentCircuit = circuit;
        s_circuits[circuit.Id] = circuit;
        return Task.CompletedTask;
    }

    public override Task OnConnectionDownAsync(Circuit circuit, CancellationToken cancellationToken)
    {
        CurrentCircuit = null;
        s_circuits.TryRemove(circuit.Id, out _);
        return Task.CompletedTask;
    }

    public override Task OnCircuitClosedAsync(Circuit circuit, CancellationToken cancellationToken)
    {
        s_circuits.TryRemove(circuit.Id, out _);
        return Task.CompletedTask;
    }
}
