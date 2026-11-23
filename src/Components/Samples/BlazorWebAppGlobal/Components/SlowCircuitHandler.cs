// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Reproduces https://github.com/dotnet/aspnetcore/issues/59657
// A CircuitHandler whose OnCircuitOpenedAsync introduces a deliberate 2-second delay.
// To observe the bug: navigate to /circuit-test, then immediately navigate away before
// the delay completes. The SignalR/WebSocket connection may be terminated and the
// circuit left in a non-recoverable state.

using Microsoft.AspNetCore.Components.Server.Circuits;

namespace BlazorWebAppGlobal.Components;

// public class BlazorCircuitHandler() : CircuitHandler
// {
//     public override async Task OnCircuitOpenedAsync(Circuit circuit, CancellationToken cancellationToken)
//     {
//         await Task.Delay(2000);
//         await base.OnCircuitOpenedAsync(circuit, cancellationToken);
//     }

//     public override async Task OnCircuitClosedAsync(Circuit circuit, CancellationToken cancellationToken)
//     {
//         await base.OnCircuitClosedAsync(circuit, cancellationToken);
//     }
// }

// internal sealed class BlazorCircuitHandler(ILogger<BlazorCircuitHandler> logger) : CircuitHandler
// {
//     public override async Task OnCircuitOpenedAsync(Circuit circuit, CancellationToken cancellationToken)
//     {
//         logger.LogInformation("[Circuit {CircuitId}] OnCircuitOpenedAsync — starting 2 s artificial delay (issue #59657)", circuit.Id);
//         await Task.Delay(2000, cancellationToken);
//         logger.LogInformation("[Circuit {CircuitId}] OnCircuitOpenedAsync — delay completed", circuit.Id);
//         await base.OnCircuitOpenedAsync(circuit, cancellationToken);
//     }

//     public override async Task OnConnectionUpAsync(Circuit circuit, CancellationToken cancellationToken)
//     {
//         //logger.LogInformation("[Circuit {CircuitId}] OnConnectionUpAsync", circuit.Id);
//         await Task.Delay(40000);
//         await base.OnConnectionUpAsync(circuit, cancellationToken);
//     }

//     public override Task OnConnectionDownAsync(Circuit circuit, CancellationToken cancellationToken)
//     {
//         logger.LogInformation("[Circuit {CircuitId}] OnConnectionDownAsync", circuit.Id);
//         return base.OnConnectionDownAsync(circuit, cancellationToken);
//     }

//     public override Task OnCircuitClosedAsync(Circuit circuit, CancellationToken cancellationToken)
//     {
//         logger.LogInformation("[Circuit {CircuitId}] OnCircuitClosedAsync", circuit.Id);
//         return base.OnCircuitClosedAsync(circuit, cancellationToken);
//     }
// }

// public class BlazorCircuitHandler() : CircuitHandler
// {
//     public override async Task OnConnectionUpAsync(
//     Circuit circuit,
//     CancellationToken cancellationToken)
// {
//     Console.WriteLine("Connection starting...");

//     // Simulate slow initializationf
//     await Task.Delay(20000);

//     Console.WriteLine("Connection completed.");
// }
// }
public class BlazorCircuitHandler() : CircuitHandler
{
    public override async Task OnCircuitOpenedAsync(Circuit circuit, CancellationToken cancellationToken)
    {
        await Task.Delay(20000);
        await base.OnCircuitOpenedAsync(circuit, cancellationToken);
    }

    public override async Task OnCircuitClosedAsync(Circuit circuit, CancellationToken cancellationToken)
    {
        await base.OnCircuitClosedAsync(circuit, cancellationToken);
    }
}