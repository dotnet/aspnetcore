// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO.Pipelines;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Connections.Features;
using Microsoft.AspNetCore.Http.Features;

namespace Microsoft.AspNetCore.Connections
{
    /// <summary>
    /// Encapsulates all information about an individual connection.
    /// </summary>
    public abstract class ConnectionContext : IAsyncDisposable
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
        public abstract IDictionary<object, object> Items { get; set; }

        /// <summary>
        /// Gets or sets the <see cref="IDuplexPipe"/> that can be used to read or write data on this connection.
        /// </summary>
        public abstract IDuplexPipe Transport { get; set; }

        /// <summary>
        /// Triggered when the client connection is closed.
        /// </summary>
        public virtual CancellationToken ConnectionClosed { get; set; }

        /// <summary>
        /// Gets or sets the local endpoint for this connection.
        /// </summary>
        public virtual EndPoint LocalEndPoint { get; set; }

        /// <summary>
        /// Gets or sets the remote endpoint for this connection.
        /// </summary>
        public virtual EndPoint RemoteEndPoint { get; set; }

        /// <summary>
        /// Aborts the underlying connection.
        /// </summary>
        /// <param name="abortReason">An optional <see cref="ConnectionAbortedException"/> describing the reason the connection is being terminated.</param>
        public virtual void Abort(ConnectionAbortedException abortReason)
        {
            // We expect this to be overridden, but this helps maintain back compat
            // with implementations of ConnectionContext that predate the addition of
            // ConnectionContext.Abort()
            Features.Get<IConnectionLifetimeFeature>()?.Abort();
        }

        /// <summary>
        /// Aborts the underlying connection.
        /// </summary>
        public virtual void Abort() => Abort(new ConnectionAbortedException("The connection was aborted by the application via ConnectionContext.Abort()."));

        /// <summary>
        /// Releases resources for the underlying connection.
        /// </summary>
        /// <returns>A <see cref="ValueTask"/> that completes when resources have been released.</returns>
        public virtual ValueTask DisposeAsync()
        {
            return default;
        }
    }
}
