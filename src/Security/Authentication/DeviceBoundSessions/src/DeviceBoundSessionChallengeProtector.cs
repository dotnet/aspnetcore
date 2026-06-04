// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Formats.Cbor;
using System.Security.Claims;
using System.Security.Cryptography;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.WebUtilities;

namespace Microsoft.AspNetCore.Authentication.DeviceBoundSessions;

internal sealed class DeviceBoundSessionChallengeProtector
{
    private readonly ITimeLimitedDataProtector _protector;

    public DeviceBoundSessionChallengeProtector(IDataProtectionProvider dataProtectionProvider)
    {
        _protector = dataProtectionProvider
            .CreateProtector("Microsoft.AspNetCore.Authentication.DeviceBoundSessions.Challenge.v1")
            .ToTimeLimitedDataProtector();
    }

    public string GenerateRegistrationChallenge(ClaimsPrincipal principal, TimeSpan lifetime)
    {
        var claimUid = ComputeClaimUid(principal);
        var writer = new CborWriter();
        writer.WriteTextString(claimUid);
        return WebEncoders.Base64UrlEncode(_protector.Protect(writer.Encode(), lifetime));
    }

    public string GenerateRefreshChallenge(ClaimsPrincipal principal, string sessionId, TimeSpan lifetime)
    {
        var claimUid = ComputeClaimUid(principal);
        var writer = new CborWriter();
        writer.WriteTextString(claimUid);
        writer.WriteTextString(sessionId);
        return WebEncoders.Base64UrlEncode(_protector.Protect(writer.Encode(), lifetime));
    }

    public bool TryValidateRegistrationChallenge(string challenge, ClaimsPrincipal principal)
    {
        if (!TryUnprotect(challenge, out var payload))
        {
            return false;
        }

        try
        {
            var reader = new CborReader(payload);
            var storedClaimUid = reader.ReadTextString();
            return string.Equals(storedClaimUid, ComputeClaimUid(principal), StringComparison.Ordinal);
        }
        catch (CborContentException)
        {
            return false;
        }
    }

    public bool TryValidateRefreshChallenge(string challenge, ClaimsPrincipal principal, string expectedSessionId)
    {
        if (!TryUnprotect(challenge, out var payload))
        {
            return false;
        }

        try
        {
            var reader = new CborReader(payload);
            var storedClaimUid = reader.ReadTextString();
            var storedSessionId = reader.ReadTextString();
            return string.Equals(storedClaimUid, ComputeClaimUid(principal), StringComparison.Ordinal) &&
                string.Equals(storedSessionId, expectedSessionId, StringComparison.Ordinal);
        }
        catch (CborContentException)
        {
            return false;
        }
    }

    private bool TryUnprotect(string challenge, out byte[] payload)
    {
        try
        {
            payload = _protector.Unprotect(WebEncoders.Base64UrlDecode(challenge));
            return true;
        }
        catch
        {
            payload = [];
            return false;
        }
    }

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