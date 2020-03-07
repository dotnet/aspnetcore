// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http.Features;

namespace Microsoft.AspNetCore.Connections
{
    /// <summary>
    /// A factory abstraction for creating connections to an endpoint.
    /// </summary>
    public interface IMultiplexedConnectionFactory
    {
        /// <summary>
        /// Creates a new connection to an endpoint.
        /// </summary>
        /// <param name="endpoint">The <see cref="EndPoint"/> to connect to.</param>
        /// <param name="features">A feature collection to pass options when connecting.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is <see cref="CancellationToken.None" />.</param>
        /// <returns>
        /// A <see cref="ValueTask{TResult}" /> that represents the asynchronous connect, yielding the <see cref="MultiplexedConnectionContext" /> for the new connection when completed.
        /// </returns>
        ValueTask<MultiplexedConnectionContext> ConnectAsync(EndPoint endpoint, IFeatureCollection features = null, CancellationToken cancellationToken = default);
    }
}
