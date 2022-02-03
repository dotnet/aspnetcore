// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure;

/// <summary>
/// Defines an interface that represents a listener bound to a specific <see cref="EndPoint"/>.
/// </summary>
internal interface IConnectionListenerBase : IAsyncDisposable
{
    /// <summary>
    /// The endpoint that was bound. This may differ from the requested endpoint, such as when the caller requested that any free port be selected.
    /// </summary>
    EndPoint EndPoint { get; }

    /// <summary>
    /// Stops listening for incoming connections.
    /// </summary>
    /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
    /// <returns>A <see cref="ValueTask"/> that represents the unbind operation.</returns>
    ValueTask UnbindAsync(CancellationToken cancellationToken = default);
}
