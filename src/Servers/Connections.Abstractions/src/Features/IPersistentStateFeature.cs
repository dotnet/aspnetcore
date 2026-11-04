// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

namespace Microsoft.AspNetCore.Connections.Features;

/// <summary>
/// Provides access to a key/value collection that can be used to persist state between connections and requests.
/// Whether a transport supports persisting state depends on the implementation. The transport must support
/// pooling and reusing connection instances for state to be persisted.
/// <para>
/// Because values added to persistent state can live in memory until a connection is no longer pooled,
/// use caution when adding items to this collection to avoid excessive memory use.
/// </para>
/// </summary>
public interface IPersistentStateFeature
{
    /// <summary>
    /// Gets a key/value collection that can be used to persist state between connections and requests.
    /// </summary>
    IDictionary<object, object?> State { get; }
}
