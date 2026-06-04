// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Formats.Cbor;
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
    /// Payload: CBOR map { "uid": claimUid }
    /// </summary>
    public static string GenerateRegistrationChallenge(
        IDataProtectionProvider dataProtectionProvider,
        ClaimsPrincipal principal,
        TimeSpan lifetime)
    {
        var protector = dataProtectionProvider.CreateProtector(ChallengePurpose).ToTimeLimitedDataProtector();
        var claimUid = ComputeClaimUid(principal);

        var writer = new CborWriter();
        writer.WriteStartMap(1);
        writer.WriteTextString("uid");
        writer.WriteTextString(claimUid);
        writer.WriteEndMap();

        var payload = writer.Encode();
        return Convert.ToBase64String(protector.Protect(payload, lifetime))
            .Replace('+', '-').Replace('/', '_').TrimEnd('=');
    }

    /// <summary>
    /// Generates a challenge for refresh (session ID is known).
    /// Payload: CBOR map { "uid": claimUid, "sid": sessionId }
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
        writer.WriteStartMap(2);
        writer.WriteTextString("uid");
        writer.WriteTextString(claimUid);
        writer.WriteTextString("sid");
        writer.WriteTextString(sessionId);
        writer.WriteEndMap();

        var payload = writer.Encode();
        return Convert.ToBase64String(protector.Protect(payload, lifetime))
            .Replace('+', '-').Replace('/', '_').TrimEnd('=');
    }

    /// <summary>
    /// Validates a registration challenge (no session ID expected).
    /// </summary>
    public static bool TryValidateRegistrationChallenge(
        IDataProtectionProvider dataProtectionProvider,
        string challenge,
        ClaimsPrincipal principal)
    {
        if (!TryUnprotectChallenge(dataProtectionProvider, challenge, out var payload))
        {
            return false;
        }

        var reader = new CborReader(payload);
        var claimUid = ReadClaimUid(reader);
        if (claimUid is null)
        {
            return false;
        }

        var expected = ComputeClaimUid(principal);
        return string.Equals(claimUid, expected, StringComparison.Ordinal);
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
        if (!TryUnprotectChallenge(dataProtectionProvider, challenge, out var payload))
        {
            return false;
        }

        var reader = new CborReader(payload);
        var (claimUid, sessionId) = ReadClaimUidAndSessionId(reader);
        if (claimUid is null || sessionId is null)
        {
            return false;
        }

        var expected = ComputeClaimUid(principal);
        return string.Equals(claimUid, expected, StringComparison.Ordinal) &&
            string.Equals(sessionId, expectedSessionId, StringComparison.Ordinal);
    }

    private static bool TryUnprotectChallenge(
        IDataProtectionProvider dataProtectionProvider,
        string challenge,
        out byte[] payload)
    {
        try
        {
            var protector = dataProtectionProvider.CreateProtector(ChallengePurpose).ToTimeLimitedDataProtector();
            // Decode URL-safe base64
            var base64 = challenge.Replace('-', '+').Replace('_', '/');
            switch (base64.Length % 4)
            {
                case 2: base64 += "=="; break;
                case 3: base64 += "="; break;
            }
            var protectedBytes = Convert.FromBase64String(base64);
            payload = protector.Unprotect(protectedBytes);
            return true;
        }
        catch (Exception) when (IsExpectedUnprotectException())
        {
            payload = [];
            return false;
        }

        // CryptographicException (expired/tampered) and FormatException (bad base64)
        static bool IsExpectedUnprotectException() => true;
    }

    private static string? ReadClaimUid(CborReader reader)
    {
        try
        {
            var mapLength = reader.ReadStartMap();
            string? claimUid = null;

            for (var i = 0; i < (mapLength ?? 0); i++)
            {
                var key = reader.ReadTextString();
                if (key == "uid")
                {
                    claimUid = reader.ReadTextString();
                }
                else
                {
                    reader.SkipValue();
                }
            }

            reader.ReadEndMap();
            return claimUid;
        }
        catch (CborContentException)
        {
            return null;
        }
    }

    private static (string? ClaimUid, string? SessionId) ReadClaimUidAndSessionId(CborReader reader)
    {
        try
        {
            var mapLength = reader.ReadStartMap();
            string? claimUid = null;
            string? sessionId = null;

            for (var i = 0; i < (mapLength ?? 0); i++)
            {
                var key = reader.ReadTextString();
                switch (key)
                {
                    case "uid":
                        claimUid = reader.ReadTextString();
                        break;
                    case "sid":
                        sessionId = reader.ReadTextString();
                        break;
                    default:
                        reader.SkipValue();
                        break;
                }
            }

            reader.ReadEndMap();
            return (claimUid, sessionId);
        }
        catch (CborContentException)
        {
            return (null, null);
        }
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

            var subClaim = identity.FindFirst(c => string.Equals("sub", c.Type, StringComparison.Ordinal));
            if (subClaim is not null && !string.IsNullOrEmpty(subClaim.Value))
            {
                return $"sub:{subClaim.Value}:{subClaim.Issuer}";
            }

            var nameIdClaim = identity.FindFirst(c => string.Equals(ClaimTypes.NameIdentifier, c.Type, StringComparison.Ordinal));
            if (nameIdClaim is not null && !string.IsNullOrEmpty(nameIdClaim.Value))
            {
                return $"nid:{nameIdClaim.Value}:{nameIdClaim.Issuer}";
            }

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

        Span<byte> hashBytes = stackalloc byte[SHA256.HashSizeInBytes];
        using var hasher = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        foreach (var claim in allClaims)
        {
            hasher.AppendData(Encoding.UTF8.GetBytes(claim.Type));
            hasher.AppendData(Encoding.UTF8.GetBytes(claim.Value));
            hasher.AppendData(Encoding.UTF8.GetBytes(claim.Issuer));
        }

        hasher.TryGetHashAndReset(hashBytes, out _);
        return WebEncoders.Base64UrlEncode(hashBytes);
    }
}
