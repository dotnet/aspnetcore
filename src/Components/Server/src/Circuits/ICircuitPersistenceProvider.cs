// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components.Server.Circuits;

// Abstraction to support persisting and restoring circuit state
internal interface ICircuitPersistenceProvider
{
    Task PersistCircuitAsync(CircuitId circuitId, PersistedCircuitState persistedCircuitState, CancellationToken cancellation = default);

    Task<PersistedCircuitState> RestoreCircuitAsync(CircuitId circuitId, CancellationToken cancellation = default);
}
