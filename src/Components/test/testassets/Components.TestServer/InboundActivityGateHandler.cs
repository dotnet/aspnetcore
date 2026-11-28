// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using BasicTestApp;
using Microsoft.AspNetCore.Components.Server.Circuits;

namespace TestServer;

// Stands in for an application handler that awaits before calling next, which lets inbound messages reach the dispatcher out of order.
public class InboundActivityGateHandler(InboundActivityGate gate) : CircuitHandler
{
    public override Func<CircuitInboundActivityContext, Task> CreateInboundActivityHandler(
        Func<CircuitInboundActivityContext, Task> next)
        => context =>
        {
            var hold = gate.TakeHold();
            return hold is null ? next(context) : WaitThenNext(hold, next, context);
        };

    private static async Task WaitThenNext(Task hold, Func<CircuitInboundActivityContext, Task> next, CircuitInboundActivityContext context)
    {
        await hold;
        await next(context);
    }
}
