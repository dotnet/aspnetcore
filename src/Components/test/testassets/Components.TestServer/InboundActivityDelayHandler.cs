// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using BasicTestApp;
using Microsoft.AspNetCore.Components.Server.Circuits;

namespace TestServer;

// Stands in for an application handler that awaits before calling next, which lets inbound messages reach the dispatcher out of order.
public class InboundActivityDelayHandler(InboundActivityDelay delay) : CircuitHandler
{
    public override Func<CircuitInboundActivityContext, Task> CreateInboundActivityHandler(
        Func<CircuitInboundActivityContext, Task> next)
        => context =>
        {
            var milliseconds = delay.TakeNextDelay();
            return milliseconds == 0 ? next(context) : DelayThenNext(milliseconds, next, context);
        };

    private static async Task DelayThenNext(int milliseconds, Func<CircuitInboundActivityContext, Task> next, CircuitInboundActivityContext context)
    {
        await Task.Delay(milliseconds);
        await next(context);
    }
}
