// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components.Server.Circuits;

namespace TestServer;

public class TestCircuitContextAccessor : CircuitHandler
{
    private readonly AsyncLocal<bool> _hasCircuitContext = new();

    public bool HasCircuitContext => _hasCircuitContext.Value;

    public override Func<CircuitInboundActivityContext, Task> CreateInboundActivityHandler(
        Func<CircuitInboundActivityContext, Task> next)
        => async (context) =>
        {
            _hasCircuitContext.Value = true;
            await next(context);
            _hasCircuitContext.Value = false;
        };
}
