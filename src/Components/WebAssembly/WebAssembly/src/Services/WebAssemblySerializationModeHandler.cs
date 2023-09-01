// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components.WebAssembly.Services;

internal class WebAssemblySerializationModeHandler : ISerializationModeHandler
{
    public PersistedStateSerializationMode GlobalSerializationMode
    {
        get => PersistedStateSerializationMode.WebAssembly;
        set => throw new NotImplementedException("Cannot change global serialization mode.");
    }

    public PersistedStateSerializationMode GetCallbackTargetSerializationMode(object? callbackTarget)
        => PersistedStateSerializationMode.WebAssembly;
}
