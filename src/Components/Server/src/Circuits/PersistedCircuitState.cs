// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components.Server.Circuits;

internal class PersistedCircuitState
{
    public Dictionary<string, byte[]> ApplicationState { get; internal set; }

    public byte[] RootComponents { get; internal set; }
}
