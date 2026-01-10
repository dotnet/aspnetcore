// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;

namespace Microsoft.AspNetCore.Components.Server.Circuits;

[DebuggerDisplay($"{{{nameof(GetDebuggerDisplay)}(),nq}}")]
internal class ResumedPersistedCircuitState
{
    public required IReadOnlyDictionary<string, byte[]> ApplicationState { get; init; }

    public required IReadOnlyDictionary<int, WebRootComponentDescriptor> RootComponentDescriptors { get; init; }

    private string GetDebuggerDisplay()
    {
        return $"ApplicationStateCount={ApplicationState?.Count ?? 0}, RootComponentCount={RootComponentDescriptors?.Count ?? 0}";
    }
}
