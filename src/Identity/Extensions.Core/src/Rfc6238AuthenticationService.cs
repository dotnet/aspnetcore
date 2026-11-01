// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System;
#if NET6_0_OR_GREATER
using System.Buffers.Binary;
#endif
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
        if (modifierBytes is not null)
        {
            return ComputeTotp(key, ApplyModifier(timestepAsBytes, modifierBytes));
        }
        return ComputeTotp(key, timestepAsBytes);
    }

    // Computes the TOTP over an input that already starts with the big-endian timestep number.
    private static int ComputeTotp(byte[] key, ReadOnlySpan<byte> input)
    {
        Span<byte> hash = stackalloc byte[HMACSHA1.HashSizeInBytes];
        var res = HMACSHA1.TryHashData(key, input, hash, out var written);
        Debug.Assert(res);
        Debug.Assert(written == hash.Length);
        return GetCode(hash);
    }
#else
        byte[] hash;
        if (modifierBytes is null)
        {
            hash = hashAlgorithm.ComputeHash(timestepAsBytes);
        }
        else
        {
            hash = hashAlgorithm.ComputeHash(ApplyModifier(timestepAsBytes, modifierBytes));
        }
        return GetCode(hash);
    }
#endif

    private static int GetCode(ReadOnlySpan<byte> hash)
    {
        // # of 0's = length of pin
        const int Mod = 1000000;

        // Generate DT string
        var offset = hash[hash.Length - 1] & 0xf;
        Debug.Assert(offset + 4 < hash.Length);
        var binaryCode = (hash[offset] & 0x7f) << 24
                            | (hash[offset + 1] & 0xff) << 16
                            | (hash[offset + 2] & 0xff) << 8
                            | (hash[offset + 3] & 0xff);

        return binaryCode % Mod;
    }

    private static byte[] ApplyModifier(ReadOnlySpan<byte> input, byte[] modifierBytes)
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

#if NET6_0_OR_GREATER
        if (modifier is null)
        {
            return ComputeTotp(securityToken, currentTimeStep, modifierBytes: null);
        }

        var input = CreateTotpInput(modifier);
        BinaryPrimitives.WriteUInt64BigEndian(input, currentTimeStep);
        return ComputeTotp(securityToken, input);
#else
        var modifierBytes = modifier is not null ? _encoding.GetBytes(modifier) : null;
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

#if NET6_0_OR_GREATER
        return ValidateCodeInWindow(securityToken, code, (long)currentTimeStep, modifier);
#else
        using (var hashAlgorithm = new HMACSHA1(securityToken))
        {
            var modifierBytes = modifier is not null ? _encoding.GetBytes(modifier) : null;
            for (var i = -2; i <= 2; i++)
            {
                var computedTotp = ComputeTotp(hashAlgorithm, (ulong)((long)currentTimeStep + i), modifierBytes);
                if (computedTotp == code)
                {
                    return true;
                }
            }
        }

        // No match
        return false;
#endif
    }

#if NET6_0_OR_GREATER
    // Checks the code against the timesteps in [currentTimeStep - 2, currentTimeStep + 2]. The modifier is encoded
    // once and a single HMAC instance is reused for the whole window, so the key is only set up once.
    internal static bool ValidateCodeInWindow(byte[] key, int code, long currentTimeStep, string? modifier)
    {
        var input = modifier is not null ? CreateTotpInput(modifier) : new byte[sizeof(long)];
        var hash = new byte[HMACSHA1.HashSizeInBytes];
        try
        {
            using var hmac = IncrementalHash.CreateHMAC(HashAlgorithmName.SHA1, key);
            for (var i = -2; i <= 2; i++)
            {
                BinaryPrimitives.WriteUInt64BigEndian(input, (ulong)(currentTimeStep + i));
                hmac.AppendData(input);
                var res = hmac.TryGetHashAndReset(hash, out var written);
                Debug.Assert(res);
                Debug.Assert(written == hash.Length);
                if (GetCode(hash) == code)
                {
                    return true;
                }
            }

            // No match
            return false;
        }
        finally
        {
            CryptographicOperations.ZeroMemory(hash);
        }
    }

    // Returns a buffer laid out as [8-byte timestep][UTF-8 modifier]; callers write the timestep before hashing.
    private static byte[] CreateTotpInput(string modifier)
    {
        var input = new byte[checked(sizeof(long) + _encoding.GetByteCount(modifier))];
        _encoding.GetBytes(modifier, 0, modifier.Length, input, sizeof(long));
        return input;
    }
#endif
}
