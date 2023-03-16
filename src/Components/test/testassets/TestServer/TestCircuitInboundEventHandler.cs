// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components.Server.Circuits;

namespace TestServer;

public class TestCircuitInboundEventHandler : CircuitHandler, ICircuitInboundEventHandler
{
    private readonly AsyncLocal<bool> _isCircuitAsyncContext = new();

    public bool IsCircuitAsyncContext => _isCircuitAsyncContext.Value;

    public async Task HandleInboundEventAsync(CircuitInboundEventContext context, CircuitInboundEventDelegate next)
    {
        _isCircuitAsyncContext.Value = true;
        await next(context);
        _isCircuitAsyncContext.Value = false;
    }
}
