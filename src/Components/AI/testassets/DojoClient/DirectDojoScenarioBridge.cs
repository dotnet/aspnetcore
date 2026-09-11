// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using DojoAgent;
using Microsoft.Extensions.AI;

namespace DojoClient;

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
