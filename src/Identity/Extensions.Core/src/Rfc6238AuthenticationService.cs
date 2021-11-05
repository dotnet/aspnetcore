// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Net;
using System.Security.Cryptography;
using System.Text;

namespace Microsoft.AspNetCore.Identity;

internal static class Rfc6238AuthenticationService
{
    private static readonly TimeSpan _timestep = TimeSpan.FromMinutes(3);
    private static readonly Encoding _encoding = new UTF8Encoding(false, true);
#if NETSTANDARD2_0 || NETFRAMEWORK
    private static readonly DateTime _unixEpoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
    private static readonly RandomNumberGenerator _rng = RandomNumberGenerator.Create();
#endif

    // Generates a new 80-bit security token
    public static byte[] GenerateRandomKey()
    {
        byte[] bytes = new byte[20];
#if NETSTANDARD2_0 || NETFRAMEWORK
        _rng.GetBytes(bytes);
#else
        RandomNumberGenerator.Fill(bytes);
#endif
        return bytes;
    }

    internal static int ComputeTotp(
#if NET6_0_OR_GREATER
        byte[] key,
#else
        HashAlgorithm hashAlgorithm,
#endif
        ulong timestepNumber,
        string modifier)
    {
        // # of 0's = length of pin
        const int Mod = 1000000;

        // See https://tools.ietf.org/html/rfc4226
        // We can add an optional modifier
        var timestepAsBytes = BitConverter.GetBytes(IPAddress.HostToNetworkOrder((long)timestepNumber));

#if NET6_0_OR_GREATER
        var hash = HMACSHA1.HashData(key, ApplyModifier(timestepAsBytes, modifier));
#else
        var hash = hashAlgorithm.ComputeHash(ApplyModifier(timestepAsBytes, modifier));
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

    private static byte[] ApplyModifier(byte[] input, string modifier)
    {
        if (string.IsNullOrEmpty(modifier))
        {
            return input;
        }

        var modifierBytes = _encoding.GetBytes(modifier);
        var combined = new byte[checked(input.Length + modifierBytes.Length)];
        Buffer.BlockCopy(input, 0, combined, 0, input.Length);
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

    public static int GenerateCode(byte[] securityToken, string modifier = null)
    {
        if (securityToken == null)
        {
            throw new ArgumentNullException(nameof(securityToken));
        }

        // Allow a variance of no greater than 9 minutes in either direction
        var currentTimeStep = GetCurrentTimeStepNumber();

#if NET6_0_OR_GREATER
        return ComputeTotp(securityToken, currentTimeStep, modifier);
#else
        using (var hashAlgorithm = new HMACSHA1(securityToken))
        {
            return ComputeTotp(hashAlgorithm, currentTimeStep, modifier);
        }
#endif
    }

    public static bool ValidateCode(byte[] securityToken, int code, string modifier = null)
    {
        if (securityToken == null)
        {
            throw new ArgumentNullException(nameof(securityToken));
        }

        // Allow a variance of no greater than 9 minutes in either direction
        var currentTimeStep = GetCurrentTimeStepNumber();

#if !NET6_0_OR_GREATER
        using (var hashAlgorithm = new HMACSHA1(securityToken))
#endif
        {
            for (var i = -2; i <= 2; i++)
            {
#if NET6_0_OR_GREATER
                var computedTotp = ComputeTotp(securityToken, (ulong)((long)currentTimeStep + i), modifier);
#else
                var computedTotp = ComputeTotp(hashAlgorithm, (ulong)((long)currentTimeStep + i), modifier);
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
