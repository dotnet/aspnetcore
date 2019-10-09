// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Security.Claims;
using System.Threading;
using Microsoft.AspNetCore.Http.Features;

namespace Microsoft.AspNetCore.SignalR
{
    /// <summary>
    /// A context abstraction for accessing information about the hub caller connection.
    /// </summary>
    public abstract class HubCallerContext
    {
        /// <summary>
        /// Gets the connection ID.
        /// </summary>
        public abstract string ConnectionId { get; }

        /// <summary>
        /// Gets the user identifier.
        /// </summary>
        public abstract string UserIdentifier { get; }

        /// <summary>
        /// Gets the user.
        /// </summary>
        public abstract ClaimsPrincipal User { get; }

        /// <summary>
        /// Gets a key/value collection that can be used to share data within the scope of this connection.
        /// </summary>
        public abstract IDictionary<object, object> Items { get; }

        /// <summary>
        /// Gets the collection of HTTP features available on the connection.
        /// </summary>
        public abstract IFeatureCollection Features { get; }

        /// <summary>
        /// Gets a <see cref="CancellationToken"/> that notifies when the connection is aborted.
        /// </summary>
        public abstract CancellationToken ConnectionAborted { get; }

        /// <summary>
        /// Aborts the connection.
        /// </summary>
        public abstract void Abort();
    }
}
