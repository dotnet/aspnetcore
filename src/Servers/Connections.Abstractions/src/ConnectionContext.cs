// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO.Pipelines;
using Microsoft.AspNetCore.Connections.Features;

namespace Microsoft.AspNetCore.Connections
{
    /// <summary>
    /// Encapsulates all information about an individual connection.
    /// </summary>
    public abstract class ConnectionContext : BaseConnectionContext, IAsyncDisposable
    {
        internal IDictionary<object, object?>? _persistentState;

        /// <summary>
        /// Gets or sets a key/value collection that can be used to persist state between connections.
        /// Whether a transport pools and reuses <see cref="ConnectionContext"/> instances and allows state to
        /// be persisted depends on the transport implementation.
        /// <para>
        /// Because values added to persistent state can live in memory until a <see cref="ConnectionContext"/>
        /// is no longer pooled, use caution with this collection to avoid excessive memory use.
        /// </para>
        /// </summary>
        public virtual IDictionary<object, object?> PersistentState
        {
            get
            {
                // Lazily allocate connection metadata
                return _persistentState ?? (_persistentState = new ConnectionItems());
            }
            set
            {
                _persistentState = value;
            }
        }

        /// <summary>
        /// Gets or sets the <see cref="IDuplexPipe"/> that can be used to read or write data on this connection.
        /// </summary>
        public abstract IDuplexPipe Transport { get; set; }

        /// <summary>
        /// Aborts the underlying connection.
        /// </summary>
        /// <param name="abortReason">An optional <see cref="ConnectionAbortedException"/> describing the reason the connection is being terminated.</param>
        public override void Abort(ConnectionAbortedException abortReason)
        {
            // We expect this to be overridden, but this helps maintain back compat
            // with implementations of ConnectionContext that predate the addition of
            // ConnectionContext.Abort()
            Features.Get<IConnectionLifetimeFeature>()?.Abort();
        }

        /// <summary>
        /// Aborts the underlying connection.
        /// </summary>
        public override void Abort() => Abort(new ConnectionAbortedException("The connection was aborted by the application via ConnectionContext.Abort()."));
    }
}
