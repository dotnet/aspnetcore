// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;

namespace DojoAgent;

/// <summary>
/// Exposes conversation-scoped controls on the host that executes the test tools.
/// </summary>
public static class FunctionScenarioEndpoints
{
    /// <summary>Maps invocation inspection and cleanup for browser tests.</summary>
    /// <param name="endpoints">The scenario host's endpoints.</param>
    public static void MapFunctionScenarioControls(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/_test/functions/{threadId}/invocations",
            (string threadId, FunctionScenarioState state) => state.GetInvocationCount(threadId));
        endpoints.MapDelete("/_test/functions/{threadId}",
            (string threadId, FunctionScenarioState state) => state.Remove(threadId));
    }
}
