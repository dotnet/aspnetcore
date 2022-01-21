// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http.Features;

namespace Microsoft.AspNetCore.Connections;

/// <summary>
/// Represents the context for a connection.
/// </summary>
public abstract class BaseConnectionContext : IAsyncDisposable
{
    /// <summary>
    /// Gets or sets a unique identifier to represent this connection in trace logs.
    /// </summary>
    public abstract string ConnectionId { get; set; }

    /// <summary>
    /// Gets the collection of features provided by the server and middleware available on this connection.
    /// </summary>
    public abstract IFeatureCollection Features { get; }

    /// <summary>
    /// Gets or sets a key/value collection that can be used to share data within the scope of this connection.
    /// </summary>
    public abstract IDictionary<object, object?> Items { get; set; }

    /// <summary>
    /// Triggered when the client connection is closed.
    /// </summary>
    public virtual CancellationToken ConnectionClosed { get; set; }

    /// <summary>
    /// Gets or sets the local endpoint for this connection.
    /// </summary>
    public virtual EndPoint? LocalEndPoint { get; set; }

    /// <summary>
    /// Gets or sets the remote endpoint for this connection.
    /// </summary>
    public virtual EndPoint? RemoteEndPoint { get; set; }

    /// <summary>
    /// Aborts the underlying connection.
    /// </summary>
    public abstract void Abort();

    /// <summary>
    /// Aborts the underlying connection.
    /// </summary>
    /// <param name="abortReason">A <see cref="ConnectionAbortedException"/> describing the reason the connection is being terminated.</param>
    public abstract void Abort(ConnectionAbortedException abortReason);

    /// <summary>
    /// Releases resources for the underlying connection.
    /// </summary>
    /// <returns>A <see cref="ValueTask"/> that completes when resources have been released.</returns>
    public virtual ValueTask DisposeAsync()
    {
        return default;
    }
}
