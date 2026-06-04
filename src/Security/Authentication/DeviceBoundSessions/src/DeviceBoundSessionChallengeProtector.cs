// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.WebUtilities;

namespace Microsoft.AspNetCore.Authentication.DeviceBoundSessions;

internal static class DeviceBoundSessionChallengeProtector
{
    private const string ChallengePurpose = "Microsoft.AspNetCore.Authentication.DeviceBoundSessions.Challenge.v1";

    /// <summary>
    /// Generates a challenge for registration (no session ID yet).
    /// </summary>
    public static string GenerateRegistrationChallenge(
        IDataProtectionProvider dataProtectionProvider,
        ClaimsPrincipal principal,
        TimeSpan lifetime)
    {
        var protector = dataProtectionProvider.CreateProtector(ChallengePurpose).ToTimeLimitedDataProtector();
        var claimUid = ComputeClaimUid(principal);
        Span<byte> nonceBytes = stackalloc byte[16];
        RandomNumberGenerator.Fill(nonceBytes);
        var nonce = WebEncoders.Base64UrlEncode(nonceBytes);
        var payload = $"{claimUid}|{nonce}";

        return protector.Protect(payload, lifetime);
    }

    /// <summary>
    /// Generates a challenge for refresh (session ID is known).
    /// </summary>
    public static string GenerateRefreshChallenge(
        IDataProtectionProvider dataProtectionProvider,
        ClaimsPrincipal principal,
        string sessionId,
        TimeSpan lifetime)
    {
        var protector = dataProtectionProvider.CreateProtector(ChallengePurpose).ToTimeLimitedDataProtector();
        var claimUid = ComputeClaimUid(principal);
        Span<byte> nonceBytes = stackalloc byte[16];
        RandomNumberGenerator.Fill(nonceBytes);
        var nonce = WebEncoders.Base64UrlEncode(nonceBytes);
        var payload = $"{claimUid}|{nonce}|{sessionId}";

        return protector.Protect(payload, lifetime);
    }

    /// <summary>
    /// Validates a registration challenge (no session ID expected).
    /// </summary>
    public static bool TryValidateRegistrationChallenge(
        IDataProtectionProvider dataProtectionProvider,
        string challenge,
        ClaimsPrincipal principal)
    {
        try
        {
            var protector = dataProtectionProvider.CreateProtector(ChallengePurpose).ToTimeLimitedDataProtector();
            var payload = protector.Unprotect(challenge);
            var parts = payload.Split('|', 2);
            if (parts.Length < 2)
            {
                return false;
            }

            var storedClaimUid = parts[0];
            return ValidateClaimUid(principal, storedClaimUid);
        }
        catch (CryptographicException)
        {
            return false;
        }
    }

    /// <summary>
    /// Validates a refresh challenge (session ID must match).
    /// </summary>
    public static bool TryValidateRefreshChallenge(
        IDataProtectionProvider dataProtectionProvider,
        string challenge,
        ClaimsPrincipal principal,
        string expectedSessionId)
    {
        try
        {
            var protector = dataProtectionProvider.CreateProtector(ChallengePurpose).ToTimeLimitedDataProtector();
            var payload = protector.Unprotect(challenge);
            var parts = payload.Split('|', 3);
            if (parts.Length != 3)
            {
                return false;
            }

            var storedClaimUid = parts[0];
            // parts[1] is the nonce (not validated, just for uniqueness)
            var storedSessionId = parts[2];

            return ValidateClaimUid(principal, storedClaimUid) &&
                string.Equals(storedSessionId, expectedSessionId, StringComparison.Ordinal);
        }
        catch (CryptographicException)
        {
            return false;
        }
    }

    private static bool ValidateClaimUid(ClaimsPrincipal principal, string expectedClaimUid)
    {
        var actualClaimUid = ComputeClaimUid(principal);
        return string.Equals(actualClaimUid, expectedClaimUid, StringComparison.Ordinal);
    }

    /// <summary>
    /// Computes a stable identifier for the user from their claims.
    /// Follows the same priority as antiforgery:
    /// 1. "sub" claim (OpenID Connect subject)
    /// 2. ClaimTypes.NameIdentifier
    /// 3. ClaimTypes.Upn
    /// 4. SHA256 hash of all claims (sorted by type)
    /// </summary>
    private static string ComputeClaimUid(ClaimsPrincipal principal)
    {
        foreach (var identity in principal.Identities)
        {
            if (!identity.IsAuthenticated)
            {
                continue;
            }

            // Try "sub" claim first (OIDC standard)
            var subClaim = identity.FindFirst(c => string.Equals("sub", c.Type, StringComparison.Ordinal));
            if (subClaim is not null && !string.IsNullOrEmpty(subClaim.Value))
            {
                return $"sub:{subClaim.Value}:{subClaim.Issuer}";
            }

            // Try NameIdentifier
            var nameIdClaim = identity.FindFirst(c => string.Equals(ClaimTypes.NameIdentifier, c.Type, StringComparison.Ordinal));
            if (nameIdClaim is not null && !string.IsNullOrEmpty(nameIdClaim.Value))
            {
                return $"nid:{nameIdClaim.Value}:{nameIdClaim.Issuer}";
            }

            // Try UPN
            var upnClaim = identity.FindFirst(c => string.Equals(ClaimTypes.Upn, c.Type, StringComparison.Ordinal));
            if (upnClaim is not null && !string.IsNullOrEmpty(upnClaim.Value))
            {
                return $"upn:{upnClaim.Value}:{upnClaim.Issuer}";
            }
        }

        // Fallback: SHA256 hash of all claims sorted by type
        var allClaims = new List<Claim>();
        foreach (var identity in principal.Identities)
        {
            if (identity.IsAuthenticated)
            {
                allClaims.AddRange(identity.Claims);
            }
        }

        if (allClaims.Count == 0)
        {
            return string.Empty;
        }

        allClaims.Sort((a, b) => string.Compare(a.Type, b.Type, StringComparison.Ordinal));

        using var hasher = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        foreach (var claim in allClaims)
        {
            hasher.AppendData(Encoding.UTF8.GetBytes(claim.Type));
            hasher.AppendData(Encoding.UTF8.GetBytes(claim.Value));
            hasher.AppendData(Encoding.UTF8.GetBytes(claim.Issuer));
        }

        Span<byte> hashBytes = stackalloc byte[32];
        hasher.TryGetHashAndReset(hashBytes, out _);
        return WebEncoders.Base64UrlEncode(hashBytes);
    }
}
