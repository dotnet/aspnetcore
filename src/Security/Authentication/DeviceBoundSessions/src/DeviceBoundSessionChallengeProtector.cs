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
    internal const string RegistrationSessionId = "registration";
    private const string ChallengePurpose = "Microsoft.AspNetCore.Authentication.DeviceBoundSessions.Challenge.v1";

    public static string GenerateChallenge(
        IDataProtectionProvider dataProtectionProvider,
        ClaimsPrincipal principal,
        string sessionId,
        TimeSpan lifetime)
    {
        var protector = dataProtectionProvider.CreateProtector(ChallengePurpose).ToTimeLimitedDataProtector();
        var claimUid = ComputeClaimUid(principal);
        var nonce = WebEncoders.Base64UrlEncode(RandomNumberGenerator.GetBytes(16));
        var payload = $"{claimUid}|{nonce}|{sessionId}";

        return protector.Protect(payload, lifetime);
    }

    public static bool TryValidateChallenge(
        IDataProtectionProvider dataProtectionProvider,
        string challenge,
        out DeviceBoundSessionChallengeMetadata metadata)
    {
        try
        {
            var protector = dataProtectionProvider.CreateProtector(ChallengePurpose).ToTimeLimitedDataProtector();
            var payload = protector.Unprotect(challenge);
            var parts = payload.Split('|', 3);
            if (parts.Length != 3)
            {
                metadata = default;
                return false;
            }

            metadata = new DeviceBoundSessionChallengeMetadata(parts[0], parts[1], parts[2]);
            return true;
        }
        catch (CryptographicException)
        {
            metadata = default;
            return false;
        }
    }

    public static bool ValidateClaimUid(ClaimsPrincipal principal, string expectedClaimUid)
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

        return WebEncoders.Base64UrlEncode(hasher.GetHashAndReset());
    }
}

internal readonly record struct DeviceBoundSessionChallengeMetadata(
    string ClaimUid,
    string Nonce,
    string SessionId);
