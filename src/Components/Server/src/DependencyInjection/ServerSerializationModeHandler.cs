// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components;

namespace Microsoft.Extensions.DependencyInjection;

internal class ServerSerializationModeHandler : ISerializationModeHandler
{
    public PersistedStateSerializationMode GetCallbackTargetSerializationMode(object? callbackTarget)
        => PersistedStateSerializationMode.Server;
}
