// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Security.Claims;
using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Connections.Features;

/// <summary>
/// Provides information about an authentication refresh.
/// </summary>
public sealed class AuthenticationRefreshContext
{
    /// <summary>
    /// Gets the <see cref="Http.HttpContext"/> for the refresh request.
    /// </summary>
    public required HttpContext HttpContext { get; init; }

    /// <summary>
    /// Gets the identifier of the connection being refreshed.
    /// </summary>
    public required string ConnectionId { get; init; }

    /// <summary>
    /// Gets the <see cref="ClaimsPrincipal"/> currently associated with the connection.
    /// </summary>
    public required ClaimsPrincipal PreviousUser { get; init; }

    /// <summary>
    /// Gets the <see cref="ClaimsPrincipal"/> produced by re-authenticating the refresh request.
    /// </summary>
    public required ClaimsPrincipal NewUser { get; init; }

    /// <summary>
    /// Gets the new authentication expiration time, or <see langword="null"/> when the authentication
    /// ticket has no expiration.
    /// </summary>
    public required DateTimeOffset? NewExpiration { get; init; }
}
