// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Security.Cryptography;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.WebUtilities;

namespace Microsoft.AspNetCore.Authentication.DeviceBoundSessions;

internal static class DeviceBoundSessionChallengeProtector
{
    internal const string RegistrationSessionId = "registration";
    private const string ChallengePurpose = "Microsoft.AspNetCore.Authentication.DeviceBoundSessions.Challenge.v1";

    public static string GenerateChallenge(
        IDataProtectionProvider dataProtectionProvider,
        string? nameIdentifier,
        string sessionId,
        TimeSpan lifetime)
    {
        var protector = dataProtectionProvider.CreateProtector(ChallengePurpose).ToTimeLimitedDataProtector();
        var nonce = WebEncoders.Base64UrlEncode(RandomNumberGenerator.GetBytes(16));
        var payload = $"{nameIdentifier ?? string.Empty}|{nonce}|{sessionId}";

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
}

internal readonly record struct DeviceBoundSessionChallengeMetadata(
    string NameIdentifier,
    string Nonce,
    string SessionId);
