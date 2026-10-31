// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net;
using Microsoft.AspNetCore.Connections;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure;

/// <summary>
/// Defines an interface that represents a listener bound to a specific <see cref="EndPoint"/>.
/// </summary>
internal interface IConnectionListener<T> : IConnectionListenerBase where T : BaseConnectionContext
{
    /// <summary>
    /// Begins an asynchronous operation to accept an incoming connection.
    /// </summary>
    /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
    /// <returns>A <see cref="ValueTask{T}"/> that completes when a connection is accepted, yielding the <see cref="BaseConnectionContext" /> representing the connection.</returns>
    ValueTask<T?> AcceptAsync(CancellationToken cancellationToken = default);
}
