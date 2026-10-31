// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;

namespace Microsoft.AspNetCore.Components.Server.Circuits;

[DebuggerDisplay($"{{{nameof(GetDebuggerDisplay)}(),nq}}")]
internal class PersistedCircuitState
{
    public IReadOnlyDictionary<string, byte[]> ApplicationState { get; set; }

    public byte[] RootComponents { get; set; }

    private string GetDebuggerDisplay()
    {
        return $"ApplicationStateCount={ApplicationState?.Count ?? 0}, RootComponentsLength={RootComponents?.Length ?? 0} bytes";
    }
}
