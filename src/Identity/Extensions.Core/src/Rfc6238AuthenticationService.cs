// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System;
using System.Diagnostics;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Shared;

namespace Microsoft.AspNetCore.Identity;

internal static class Rfc6238AuthenticationService
{
    private static readonly TimeSpan _timestep = TimeSpan.FromMinutes(3);
    private static readonly Encoding _encoding = new UTF8Encoding(false, true);
#if NETSTANDARD2_0 || NETFRAMEWORK
    private static readonly DateTime _unixEpoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
#endif

    internal static int ComputeTotp(
#if NET6_0_OR_GREATER
        byte[] key,
#else
        HashAlgorithm hashAlgorithm,
#endif
        ulong timestepNumber,
        byte[]? modifierBytes)
    {
        // # of 0's = length of pin
        const int Mod = 1000000;

        // See https://tools.ietf.org/html/rfc4226
        // We can add an optional modifier
#if NET6_0_OR_GREATER
        Span<byte> timestepAsBytes = stackalloc byte[sizeof(long)];
        var res = BitConverter.TryWriteBytes(timestepAsBytes, IPAddress.HostToNetworkOrder((long)timestepNumber));
        Debug.Assert(res);
#else
        var timestepAsBytes = BitConverter.GetBytes(IPAddress.HostToNetworkOrder((long)timestepNumber));
#endif

#if NET6_0_OR_GREATER
        Span<byte> modifierCombinedBytes = timestepAsBytes;
        if (modifierBytes is not null)
        {
            modifierCombinedBytes = ApplyModifier(timestepAsBytes, modifierBytes);
        }
        Span<byte> hash = stackalloc byte[HMACSHA1.HashSizeInBytes];
        res = HMACSHA1.TryHashData(key, modifierCombinedBytes, hash, out var written);
        Debug.Assert(res);
        Debug.Assert(written == hash.Length);
#else
        var hash = hashAlgorithm.ComputeHash(ApplyModifier(timestepAsBytes, modifierBytes));
#endif

        // Generate DT string
        var offset = hash[hash.Length - 1] & 0xf;
        Debug.Assert(offset + 4 < hash.Length);
        var binaryCode = (hash[offset] & 0x7f) << 24
                            | (hash[offset + 1] & 0xff) << 16
                            | (hash[offset + 2] & 0xff) << 8
                            | (hash[offset + 3] & 0xff);

        return binaryCode % Mod;
    }

    private static byte[] ApplyModifier(Span<byte> input, byte[] modifierBytes)
    {
        var combined = new byte[checked(input.Length + modifierBytes.Length)];
        input.CopyTo(combined);
        Buffer.BlockCopy(modifierBytes, 0, combined, input.Length, modifierBytes.Length);
        return combined;
    }

    // More info: https://tools.ietf.org/html/rfc6238#section-4
    private static ulong GetCurrentTimeStepNumber()
    {
#if NETSTANDARD2_0 || NETFRAMEWORK
        var delta = DateTime.UtcNow - _unixEpoch;
#else
        var delta = DateTimeOffset.UtcNow - DateTimeOffset.UnixEpoch;
#endif
        return (ulong)(delta.Ticks / _timestep.Ticks);
    }

    public static int GenerateCode(byte[] securityToken, string? modifier = null)
    {
        ArgumentNullThrowHelper.ThrowIfNull(securityToken);

        // Allow a variance of no greater than 9 minutes in either direction
        var currentTimeStep = GetCurrentTimeStepNumber();

        var modifierBytes = modifier is not null ? _encoding.GetBytes(modifier) : null;
#if NET6_0_OR_GREATER
        return ComputeTotp(securityToken, currentTimeStep, modifierBytes);
#else
        using (var hashAlgorithm = new HMACSHA1(securityToken))
        {
            return ComputeTotp(hashAlgorithm, currentTimeStep, modifierBytes);
        }
#endif
    }

    public static bool ValidateCode(byte[] securityToken, int code, string? modifier = null)
    {
        ArgumentNullThrowHelper.ThrowIfNull(securityToken);

        // Allow a variance of no greater than 9 minutes in either direction
        var currentTimeStep = GetCurrentTimeStepNumber();

#if !NET6_0_OR_GREATER
        using (var hashAlgorithm = new HMACSHA1(securityToken))
#endif
        {
            var modifierBytes = modifier is not null ? _encoding.GetBytes(modifier) : null;
            for (var i = -2; i <= 2; i++)
            {
#if NET6_0_OR_GREATER
                var computedTotp = ComputeTotp(securityToken, (ulong)((long)currentTimeStep + i), modifierBytes);
#else
                var computedTotp = ComputeTotp(hashAlgorithm, (ulong)((long)currentTimeStep + i), modifierBytes);
#endif
                if (computedTotp == code)
                {
                    return true;
                }
            }
        }

        // No match
        return false;
    }
}
