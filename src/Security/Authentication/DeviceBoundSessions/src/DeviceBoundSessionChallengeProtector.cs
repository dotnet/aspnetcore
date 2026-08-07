// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Formats.Cbor;
using System.Security.Claims;
using System.Security.Cryptography;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Authentication.DeviceBoundSessions;

internal sealed class DeviceBoundSessionChallengeProtector
{
    private readonly ITimeLimitedDataProtector _registrationProtector;
    private readonly ITimeLimitedDataProtector _refreshProtector;
    private readonly ILogger<DeviceBoundSessionChallengeProtector> _logger;

    public DeviceBoundSessionChallengeProtector(IDataProtectionProvider dataProtectionProvider, ILogger<DeviceBoundSessionChallengeProtector> logger)
    {
        _logger = logger;

        // Registration and refresh challenges use distinct data-protection purposes so a challenge
        // minted for one flow cannot be decrypted (let alone accepted) by the other. This makes
        // cross-type challenge confusion cryptographically impossible, independent of payload shape.
        _registrationProtector = dataProtectionProvider
            .CreateProtector("Microsoft.AspNetCore.Authentication.DeviceBoundSessions.Challenge.Registration.v1")
            .ToTimeLimitedDataProtector();
        _refreshProtector = dataProtectionProvider
            .CreateProtector("Microsoft.AspNetCore.Authentication.DeviceBoundSessions.Challenge.Refresh.v1")
            .ToTimeLimitedDataProtector();
    }

    public string GenerateRegistrationChallenge(ClaimsPrincipal principal, TimeSpan lifetime)
    {
        var claimUid = ComputeClaimUid(principal);
        var writer = new CborWriter(allowMultipleRootLevelValues: true);
        writer.WriteTextString(claimUid);
        return WebEncoders.Base64UrlEncode(_registrationProtector.Protect(writer.Encode(), lifetime));
    }

    public string GenerateRefreshChallenge(ClaimsPrincipal principal, string sessionId, TimeSpan lifetime)
    {
        var claimUid = ComputeClaimUid(principal);
        var writer = new CborWriter(allowMultipleRootLevelValues: true);
        writer.WriteTextString(claimUid);
        writer.WriteTextString(sessionId);
        return WebEncoders.Base64UrlEncode(_refreshProtector.Protect(writer.Encode(), lifetime));
    }

    public bool TryValidateRegistrationChallenge(string challenge, ClaimsPrincipal principal)
    {
        if (!TryUnprotect(_registrationProtector, challenge, out var payload))
        {
            // Expired, tampered, or minted for a different flow/version — undecryptable.
            _logger.RegistrationChallengeUndecryptable();
            return false;
        }

        try
        {
            var reader = new CborReader(payload, allowMultipleRootLevelValues: true);
            var storedClaimUid = reader.ReadTextString();
            if (reader.PeekState() != CborReaderState.Finished)
            {
                // Extra trailing data — not a registration challenge in the shape this version writes.
                _logger.RegistrationChallengeMalformed();
                return false;
            }

            if (!string.Equals(storedClaimUid, ComputeClaimUid(principal), StringComparison.Ordinal))
            {
                // Decrypted but bound to a different principal than the request.
                _logger.RegistrationChallengePrincipalMismatch();
                return false;
            }

            return true;
        }
        catch (Exception ex) when (ex is CborContentException or InvalidOperationException)
        {
            // Decrypted (so authentic) but not in the expected registration-challenge shape — e.g. a
            // challenge minted by a different/older serialization version. Reading the wrong type throws
            // InvalidOperationException; genuinely-malformed CBOR throws CborContentException.
            _logger.RegistrationChallengeMalformed();
            return false;
        }
    }

    public bool TryValidateRefreshChallenge(string challenge, ClaimsPrincipal principal, string expectedSessionId)
    {
        if (!TryUnprotect(_refreshProtector, challenge, out var payload))
        {
            // Expired, tampered, or minted for a different flow/version — undecryptable.
            _logger.RefreshChallengeUndecryptable(expectedSessionId);
            return false;
        }

        try
        {
            var reader = new CborReader(payload, allowMultipleRootLevelValues: true);
            var storedClaimUid = reader.ReadTextString();
            var storedSessionId = reader.ReadTextString();
            if (reader.PeekState() != CborReaderState.Finished)
            {
                // Extra trailing data — not a refresh challenge in the shape this version writes.
                _logger.RefreshChallengeMalformed(expectedSessionId);
                return false;
            }

            if (!string.Equals(storedClaimUid, ComputeClaimUid(principal), StringComparison.Ordinal))
            {
                // Decrypted but bound to a different principal than the request.
                _logger.RefreshChallengePrincipalMismatch(expectedSessionId);
                return false;
            }

            if (!string.Equals(storedSessionId, expectedSessionId, StringComparison.Ordinal))
            {
                // Decrypted but bound to a different session than the request.
                _logger.RefreshChallengeSessionMismatch(expectedSessionId);
                return false;
            }

            return true;
        }
        catch (Exception ex) when (ex is CborContentException or InvalidOperationException)
        {
            // Decrypted (so authentic) but not in the expected refresh-challenge shape — e.g. a challenge
            // minted by a different/older serialization version, or a registration challenge (one field)
            // replayed as a refresh proof. Reading the wrong type or past the end throws
            // InvalidOperationException; genuinely-malformed CBOR (practically unreachable after an
            // authenticated decrypt) throws CborContentException. Either way the challenge is unusable.
            _logger.RefreshChallengeMalformed(expectedSessionId);
            return false;
        }
    }

    private static bool TryUnprotect(ITimeLimitedDataProtector protector, string challenge, out byte[] payload)
    {
        try
        {
            payload = protector.Unprotect(WebEncoders.Base64UrlDecode(challenge));
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
        var writer = new CborWriter(allowMultipleRootLevelValues: true);
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

        var writer = new CborWriter(allowMultipleRootLevelValues: true);
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
