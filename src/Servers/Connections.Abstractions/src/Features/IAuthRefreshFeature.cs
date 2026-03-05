// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.Connections.Features;

/// <summary>
/// Feature for refreshing authentication on a connection.
/// When present, indicates the connection supports auth token refresh.
/// </summary>
public interface IAuthRefreshFeature
{
    /// <summary>
    /// Gets the initial token lifetime in seconds as provided by the server during negotiation.
    /// Null if the server did not provide a token lifetime.
    /// </summary>
    int? InitialTokenLifetimeSeconds { get; }

    /// <summary>
    /// Sends a refresh request to the server with new authentication credentials.
    /// Returns the updated token lifetime in seconds, or null if not provided.
    /// </summary>
    Task<int?> RefreshAuthAsync(CancellationToken cancellationToken = default);
}
