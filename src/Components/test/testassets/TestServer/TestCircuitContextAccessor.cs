// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components.Server.Circuits;

namespace TestServer;

public class TestCircuitContextAccessor : CircuitHandler, ICircuitInboundEventHandler
{
    private readonly AsyncLocal<bool> _hasCircuitContext = new();

    public bool HasCircuitContext => _hasCircuitContext.Value;

    public async Task HandleInboundEventAsync(CircuitInboundEventContext context, CircuitInboundEventDelegate next)
    {
        _hasCircuitContext.Value = true;
        await next(context);
        _hasCircuitContext.Value = false;
    }
}
