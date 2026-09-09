// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using AGUI.Abstractions;
using DojoAgent;
using Microsoft.Extensions.AI;

namespace DojoClient;

internal sealed class DojoBackend
{
    internal DojoBackend(string? value)
    {
        IsDirect = value switch
        {
            null or "AGUI" => false,
            "Direct" => true,
            _ => throw new InvalidOperationException(
                $"Invalid DOJO_BACKEND '{value}'. Expected 'AGUI' or 'Direct'."),
        };
    }

    internal bool IsDirect { get; }

    internal ChatOptions CreateStateOptions(Func<DojoRequestContext> getContext)
    {
        if (IsDirect)
        {
            return new ChatOptions
            {
                AdditionalProperties = new()
                {
                    [DojoRequestContext.PropertyName] = getContext,
                },
            };
        }

        return new ChatOptions
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
}
