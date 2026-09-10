// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using DojoAgent;
using Microsoft.Extensions.AI;

namespace DojoClient;

/// <summary>Bridges a dojo scenario to the in-process direct <see cref="IChatClient"/> transport.</summary>
internal sealed class DirectDojoScenarioBridge : IDojoScenarioBridge
{
    public ChatOptions CreateStateOptions(Func<DojoRequestContext> getContext) => new()
    {
        AdditionalProperties = new()
        {
            [DojoRequestContext.PropertyName] = getContext,
        },
    };
}
