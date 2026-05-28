// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Security.Claims;

namespace Microsoft.AspNetCore.Http.Connections;

/// <summary>
/// Provides information about an in-flight authentication refresh to a
/// <see cref="HttpConnectionDispatcherOptions.OnAuthRefresh"/> callback so the application
/// can accept or reject the refresh.
/// </summary>
public sealed class AuthRefreshContext
{
    /// <summary>
    /// The <see cref="Http.HttpContext"/> for the refresh request.
    /// </summary>
    public required HttpContext HttpContext { get; init; }

    /// <summary>
    /// The id of the connection being refreshed.
    /// </summary>
    public required string ConnectionId { get; init; }

    /// <summary>
    /// The <see cref="ClaimsPrincipal"/> currently associated with the connection
    /// (before this refresh is applied).
    /// </summary>
    public required ClaimsPrincipal PreviousUser { get; init; }

    /// <summary>
    /// The <see cref="ClaimsPrincipal"/> produced by re-authenticating the refresh request.
    /// This is the principal that will replace <see cref="PreviousUser"/> on the connection
    /// if the callback returns <c>true</c>.
    /// </summary>
    public required ClaimsPrincipal NewUser { get; init; }

    /// <summary>
    /// The new authentication expiration time, or <see cref="DateTimeOffset.MaxValue"/>
    /// when the authentication ticket has no expiration.
    /// </summary>
    public required DateTimeOffset NewExpiration { get; init; }

    /// <summary>
    /// When the callback returns <c>false</c>, this value is returned as the
    /// <c>error_description</c> in the response body. Optional.
    /// </summary>
    public string? DenyReason { get; set; }
}
