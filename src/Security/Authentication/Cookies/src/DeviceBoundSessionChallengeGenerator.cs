// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Security.Cryptography;
using Microsoft.AspNetCore.DataProtection;

namespace Microsoft.AspNetCore.Authentication.Cookies;

/// <summary>
/// Generates and validates self-contained DBSC challenges using Data Protection.
/// Challenges are stateless — the server can validate them without storing them.
/// </summary>
internal sealed class DeviceBoundSessionChallengeGenerator
{
    private readonly IDataProtector _protector;
    private readonly TimeProvider _timeProvider;

    public DeviceBoundSessionChallengeGenerator(IDataProtectionProvider dataProtectionProvider, TimeProvider? timeProvider = null)
    {
        _protector = dataProtectionProvider.CreateProtector("Microsoft.AspNetCore.Authentication.Cookies.DBSC.Challenge");
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    /// <summary>
    /// Generates a self-contained challenge string for the given session.
    /// </summary>
    /// <param name="sessionId">The session identifier.</param>
    /// <returns>A data-protected challenge string.</returns>
    public string GenerateChallenge(string sessionId)
    {
        var nonce = RandomNumberGenerator.GetBytes(16);
        var timestamp = _timeProvider.GetUtcNow().ToUnixTimeSeconds();

        // Format: timestamp|nonce_base64|sessionId
        var payload = $"{timestamp}|{Convert.ToBase64String(nonce)}|{sessionId}";
        return _protector.Protect(payload);
    }

    /// <summary>
    /// Validates a challenge string and checks that it was issued recently and for the correct session.
    /// </summary>
    /// <param name="challenge">The challenge string to validate.</param>
    /// <param name="expectedSessionId">The expected session identifier.</param>
    /// <param name="maxAge">The maximum age of the challenge.</param>
    /// <returns><c>true</c> if the challenge is valid and fresh; otherwise, <c>false</c>.</returns>
    public bool ValidateChallenge(string challenge, string expectedSessionId, TimeSpan maxAge)
    {
        string payload;
        try
        {
            payload = _protector.Unprotect(challenge);
        }
        catch (CryptographicException)
        {
            return false;
        }

        var parts = payload.Split('|', 3);
        if (parts.Length != 3)
        {
            return false;
        }

        if (!long.TryParse(parts[0], out var timestamp))
        {
            return false;
        }

        // Check freshness
        var issued = DateTimeOffset.FromUnixTimeSeconds(timestamp);
        var now = _timeProvider.GetUtcNow();
        if (now - issued > maxAge)
        {
            return false;
        }

        // Check session ID
        var sessionId = parts[2];
        return string.Equals(sessionId, expectedSessionId, StringComparison.Ordinal);
    }
}
