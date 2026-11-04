// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading;

namespace Microsoft.AspNetCore.Connections.Features;

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
