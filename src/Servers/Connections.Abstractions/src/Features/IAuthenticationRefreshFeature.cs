// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.Connections.Features;

/// <summary>
/// Feature for refreshing authentication on a connection.
/// When present, indicates the connection supports authentication token refresh.
/// </summary>
public interface IAuthenticationRefreshFeature
{
    /// <summary>
    /// Gets the initial token lifetime as provided by the server during negotiation.
    /// Null if the server did not provide a token lifetime.
    /// </summary>
    TimeSpan? InitialTokenLifetime { get; }

    /// <summary>
    /// Sends a refresh request to the server with new authentication credentials.
    /// Returns the updated token lifetime, or null if not provided.
    /// </summary>
    Task<TimeSpan?> RefreshAuthenticationAsync(CancellationToken cancellationToken = default);
}
