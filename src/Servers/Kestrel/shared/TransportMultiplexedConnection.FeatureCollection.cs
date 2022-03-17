// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using Microsoft.AspNetCore.Connections.Features;

namespace Microsoft.AspNetCore.Connections;

internal partial class TransportMultiplexedConnection
{
    // NOTE: When feature interfaces are added to or removed from this TransportConnection class implementation,
    // then the list of `features` in the generated code project MUST also be updated first
    // and the code generator re-reun, which will change the interface list.
    // See also: tools/CodeGenerator/TransportConnectionFeatureCollection.cs

    MemoryPool<byte> IMemoryPoolFeature.MemoryPool => MemoryPool;

    IDictionary<object, object?> IConnectionItemsFeature.Items
    {
        get => Items;
        set => Items = value;
    }

    CancellationToken IConnectionLifetimeFeature.ConnectionClosed
    {
        get => ConnectionClosed;
        set => ConnectionClosed = value;
    }

    void IConnectionLifetimeFeature.Abort() => Abort(new ConnectionAbortedException("The connection was aborted by the application via IConnectionLifetimeFeature.Abort()."));
}
