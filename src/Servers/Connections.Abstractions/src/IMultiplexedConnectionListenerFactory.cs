// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.Connections
{
    /// <summary>
    /// Defines an interface that provides the mechanisms for binding to various types of <see cref="EndPoint"/>s.
    /// </summary>
    public interface IMultiplexedConnectionListenerFactory
    {
        /// <summary>
        /// Creates an <see cref="IMultiplexedConnectionListener"/> bound to the specified <see cref="EndPoint"/>.
        /// </summary>
        /// <param name="endpoint">The <see cref="EndPoint" /> to bind to.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        /// <returns>A <see cref="ValueTask{IMultiplexedConnectionListener}"/> that completes when the listener has been bound, yielding a <see cref="IMultiplexedConnectionListener" /> representing the new listener.</returns>
        ValueTask<IMultiplexedConnectionListener> BindAsync(EndPoint endpoint, CancellationToken cancellationToken = default);
    }
}
