// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using AGUI.Abstractions;
using DojoAgent;
using Microsoft.Extensions.AI;

namespace DojoClient;

/// <summary>Bridges a dojo scenario to the AG-UI HTTP/SSE transport.</summary>
internal sealed class AGUIDojoScenarioBridge : IDojoScenarioBridge
{
    public ChatOptions CreateStateOptions(Func<DojoRequestContext> getContext) => new()
    {
        RawRepresentationFactory = _ =>
        {
            var context = getContext();
            return new RunAgentInput
            {
                ThreadId = context.ThreadId,
                State = context.State,
            };
        },
    };
}
