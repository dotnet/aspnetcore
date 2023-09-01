// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components.WebAssembly.Services;

internal class WebAssemblySerializationModeHandler : ISerializationModeHandler
{
    public PersistedStateSerializationMode GetCallbackTargetSerializationMode(object? callbackTarget)
        => PersistedStateSerializationMode.WebAssembly;
}
