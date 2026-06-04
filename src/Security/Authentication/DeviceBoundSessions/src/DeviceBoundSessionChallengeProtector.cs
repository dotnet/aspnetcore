// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Formats.Cbor;
using System.Security.Claims;
using System.Security.Cryptography;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.WebUtilities;

namespace Microsoft.AspNetCore.Authentication.DeviceBoundSessions;

internal static class DeviceBoundSessionChallengeProtector
{
    private const string ChallengePurpose = "Microsoft.AspNetCore.Authentication.DeviceBoundSessions.Challenge.v1";

    /// <summary>
    /// Generates a challenge for registration (no session ID yet).
    /// Payload: CBOR byte string (claimUid)
    /// </summary>
    public static string GenerateRegistrationChallenge(
        IDataProtectionProvider dataProtectionProvider,
        ClaimsPrincipal principal,
        TimeSpan lifetime)
    {
        var protector = dataProtectionProvider.CreateProtector(ChallengePurpose).ToTimeLimitedDataProtector();
        var claimUid = ComputeClaimUid(principal);

        var writer = new CborWriter();
        writer.WriteTextString(claimUid);

        var protectedBytes = protector.Protect(writer.Encode(), lifetime);
        return WebEncoders.Base64UrlEncode(protectedBytes);
    }

    /// <summary>
    /// Generates a challenge for refresh (session ID is known).
    /// Payload: CBOR sequence (claimUid, sessionId)
    /// </summary>
    public static string GenerateRefreshChallenge(
        IDataProtectionProvider dataProtectionProvider,
        ClaimsPrincipal principal,
        string sessionId,
        TimeSpan lifetime)
    {
        var protector = dataProtectionProvider.CreateProtector(ChallengePurpose).ToTimeLimitedDataProtector();
        var claimUid = ComputeClaimUid(principal);

        var writer = new CborWriter();
        writer.WriteTextString(claimUid);
        writer.WriteTextString(sessionId);

        var protectedBytes = protector.Protect(writer.Encode(), lifetime);
        return WebEncoders.Base64UrlEncode(protectedBytes);
    }

    /// <summary>
    /// Validates a registration challenge (no session ID expected).
    /// </summary>
    public static bool TryValidateRegistrationChallenge(
        IDataProtectionProvider dataProtectionProvider,
        string challenge,
        ClaimsPrincipal principal)
    {
        if (!TryUnprotect(dataProtectionProvider, challenge, out var payload))
        {
            return false;
        }

        try
        {
            var reader = new CborReader(payload);
            var storedClaimUid = reader.ReadTextString();

            var expected = ComputeClaimUid(principal);
            return string.Equals(storedClaimUid, expected, StringComparison.Ordinal);
        }
        catch (CborContentException)
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
        if (!TryUnprotect(dataProtectionProvider, challenge, out var payload))
        {
            return false;
        }

        try
        {
            var reader = new CborReader(payload);
            var storedClaimUid = reader.ReadTextString();
            var storedSessionId = reader.ReadTextString();

            var expected = ComputeClaimUid(principal);
            return string.Equals(storedClaimUid, expected, StringComparison.Ordinal) &&
                string.Equals(storedSessionId, expectedSessionId, StringComparison.Ordinal);
        }
        catch (CborContentException)
        {
            return false;
        }
    }

    private static bool TryUnprotect(IDataProtectionProvider dataProtectionProvider, string challenge, out byte[] payload)
    {
        try
        {
            var protector = dataProtectionProvider.CreateProtector(ChallengePurpose).ToTimeLimitedDataProtector();
            var protectedBytes = WebEncoders.Base64UrlDecode(challenge);
            payload = protector.Unprotect(protectedBytes);
            return true;
        }
        catch
        {
            payload = [];
            return false;
        }
    }

    /// <summary>
    /// Computes a stable identifier for the user from their claims.
    /// Priority: sub > NameIdentifier > UPN > SHA256(all claims as CBOR).
    /// </summary>
    internal static string ComputeClaimUid(ClaimsPrincipal principal)
    {
        foreach (var identity in principal.Identities)
        {
            if (!identity.IsAuthenticated)
            {
                continue;
            }

            var subClaim = identity.FindFirst(c => string.Equals("sub", c.Type, StringComparison.Ordinal));
            if (subClaim is not null && !string.IsNullOrEmpty(subClaim.Value))
            {
                return EncodeClaim(subClaim);
            }

            var nameIdClaim = identity.FindFirst(c => string.Equals(ClaimTypes.NameIdentifier, c.Type, StringComparison.Ordinal));
            if (nameIdClaim is not null && !string.IsNullOrEmpty(nameIdClaim.Value))
            {
                return EncodeClaim(nameIdClaim);
            }

            var upnClaim = identity.FindFirst(c => string.Equals(ClaimTypes.Upn, c.Type, StringComparison.Ordinal));
            if (upnClaim is not null && !string.IsNullOrEmpty(upnClaim.Value))
            {
                return EncodeClaim(upnClaim);
            }
        }

        // Fallback: SHA256 hash of all claims encoded as CBOR
        return ComputeClaimHashFallback(principal);
    }

    private static string EncodeClaim(Claim claim)
    {
        var writer = new CborWriter();
        writer.WriteTextString(claim.Type);
        writer.WriteTextString(claim.Value);
        writer.WriteTextString(claim.Issuer);
        return WebEncoders.Base64UrlEncode(writer.Encode());
    }

    private static string ComputeClaimHashFallback(ClaimsPrincipal principal)
    {
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

        // Encode all claims as a CBOR sequence, then hash
        var writer = new CborWriter();
        foreach (var claim in allClaims)
        {
            writer.WriteTextString(claim.Type);
            writer.WriteTextString(claim.Value);
            writer.WriteTextString(claim.Issuer);
        }

        var encoded = writer.Encode();
        Span<byte> hashBytes = stackalloc byte[SHA256.HashSizeInBytes];
        SHA256.HashData(encoded, hashBytes);
        return WebEncoders.Base64UrlEncode(hashBytes);
    }
}
