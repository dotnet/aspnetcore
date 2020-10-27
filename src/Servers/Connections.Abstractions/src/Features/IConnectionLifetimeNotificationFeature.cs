// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading;

namespace Microsoft.AspNetCore.Connections.Features
{
    /// <summary>
    /// Enables graceful termination of the connection.
    /// </summary>
    public interface IConnectionLifetimeNotificationFeature
    {
        /// <summary>
        /// Gets or set an <see cref="CancellationToken"/> that will be triggered when closing the connection has been requested.
        /// </summary>
        CancellationToken ConnectionClosedRequested { get; set; }

        /// <summary>
        /// Requests the connection to be closed.
        /// </summary>
        void RequestClose();
    }
}
