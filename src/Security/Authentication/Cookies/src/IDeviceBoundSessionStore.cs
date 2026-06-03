// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Authentication.Cookies;

/// <summary>
/// An optional interface for server-side storage of Device Bound Session data.
/// This is not required for basic DBSC operation (the public key is stored in the long-lived cookie),
/// but can be used for session revocation and tracking scenarios.
/// </summary>
/// <remarks>
/// <para>
/// By default, DBSC operates statelessly: the public key is embedded in the data-protected
/// long-lived cookie, and challenges are self-contained. This interface enables additional
/// server-side capabilities:
/// </para>
/// <list type="bullet">
/// <item><description>Session revocation: explicitly revoke a bound session before cookie expiry.</description></item>
/// <item><description>Audit: track active device-bound sessions per user.</description></item>
/// <item><description>Key rotation: force re-registration by revoking the current session.</description></item>
/// </list>
/// </remarks>
public interface IDeviceBoundSessionStore
{
    /// <summary>
    /// Stores a device-bound session record, associating the session identifier with metadata.
    /// </summary>
    /// <param name="sessionId">The unique session identifier.</param>
    /// <param name="publicKeyJwk">The JSON Web Key (JWK) of the device's public key.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task StoreAsync(string sessionId, string publicKeyJwk, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks whether a device-bound session has been revoked.
    /// </summary>
    /// <param name="sessionId">The session identifier to check.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns><c>true</c> if the session has been revoked; otherwise, <c>false</c>.</returns>
    Task<bool> IsRevokedAsync(string sessionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Revokes a device-bound session. After revocation, refresh attempts will be rejected
    /// and the browser session will be terminated.
    /// </summary>
    /// <param name="sessionId">The session identifier to revoke.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task RevokeAsync(string sessionId, CancellationToken cancellationToken = default);
}
