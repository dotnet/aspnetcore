// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
#if NET
using System.Buffers;
using System.Buffers.Binary;
#endif
using System.Diagnostics.CodeAnalysis;
using System.Security.Cryptography;
using System.Threading;
using Microsoft.AspNetCore.DataProtection.Extensions;
using Microsoft.AspNetCore.Shared;

namespace Microsoft.AspNetCore.DataProtection;

/// <summary>
/// Wraps an existing <see cref="IDataProtector"/> and appends a purpose that allows
/// protecting data with a finite lifetime.
/// </summary>
internal sealed class TimeLimitedDataProtector : ITimeLimitedDataProtector
{
    private const string MyPurposeString = "Microsoft.AspNetCore.DataProtection.TimeLimitedDataProtector.v1";
    private const int ExpirationTimeHeaderSize = 8; // size of the expiration time header in bytes (64-bit UTC tick count)

    private readonly IDataProtector _innerProtector;
    private IDataProtector? _innerProtectorWithTimeLimitedPurpose; // created on-demand

    public TimeLimitedDataProtector(IDataProtector innerProtector)
    {
        _innerProtector = innerProtector;
    }

    public ITimeLimitedDataProtector CreateProtector(string purpose)
    {
        ArgumentNullThrowHelper.ThrowIfNull(purpose);

        return new TimeLimitedDataProtector(_innerProtector.CreateProtector(purpose));
    }

    private IDataProtector GetInnerProtectorWithTimeLimitedPurpose()
    {
        // thread-safe lazy init pattern with multi-execution and single publication
        var retVal = Volatile.Read(ref _innerProtectorWithTimeLimitedPurpose);
        if (retVal == null)
        {
            var newValue = _innerProtector.CreateProtector(MyPurposeString); // we always append our purpose to the end of the chain
            retVal = Interlocked.CompareExchange(ref _innerProtectorWithTimeLimitedPurpose, newValue, null) ?? newValue;
        }
        return retVal;
    }

    public byte[] Protect(byte[] plaintext, DateTimeOffset expiration)
    {
        ArgumentNullThrowHelper.ThrowIfNull(plaintext);

        var innerProtector = GetInnerProtectorWithTimeLimitedPurpose();
#if NET
        if (innerProtector is ISpanDataProtector spanProtector)
        {
            return ProtectWithSpanProtector(spanProtector, plaintext, expiration);
        }
#endif

        // We prepend the expiration time (as a 64-bit UTC tick count) to the unprotected data.
        byte[] plaintextWithHeader = new byte[checked(ExpirationTimeHeaderSize + plaintext.Length)];
        BitHelpers.WriteUInt64(plaintextWithHeader, 0, (ulong)expiration.UtcTicks);
        Buffer.BlockCopy(plaintext, 0, plaintextWithHeader, ExpirationTimeHeaderSize, plaintext.Length);

        return innerProtector.Protect(plaintextWithHeader);
    }

    public byte[] Unprotect(byte[] protectedData, out DateTimeOffset expiration)
    {
        ArgumentNullThrowHelper.ThrowIfNull(protectedData);

        return UnprotectCore(protectedData, DateTimeOffset.UtcNow, out expiration);
    }

    internal byte[] UnprotectCore(byte[] protectedData, DateTimeOffset now, out DateTimeOffset expiration)
    {
        ArgumentNullThrowHelper.ThrowIfNull(protectedData);

        try
        {
            var innerProtector = GetInnerProtectorWithTimeLimitedPurpose();
#if NET
            if (innerProtector is ISpanDataProtector spanProtector)
            {
                return UnprotectWithSpanProtector(spanProtector, protectedData, now, out expiration);
            }
#endif

            byte[] plaintextWithHeader = innerProtector.Unprotect(protectedData);
            if (plaintextWithHeader.Length < ExpirationTimeHeaderSize)
            {
                // header isn't present
                throw new CryptographicException(Resources.TimeLimitedDataProtector_PayloadInvalid);
            }

            // Read expiration time back out of the payload
            ulong utcTicksExpiration = BitHelpers.ReadUInt64(plaintextWithHeader, 0);
            DateTimeOffset embeddedExpiration = new DateTimeOffset(checked((long)utcTicksExpiration), TimeSpan.Zero /* UTC */);

            // Are we expired?
            if (now > embeddedExpiration)
            {
                throw new CryptographicException(Resources.FormatTimeLimitedDataProtector_PayloadExpired(embeddedExpiration));
            }

            // Not expired - split and return payload
            byte[] retVal = new byte[plaintextWithHeader.Length - ExpirationTimeHeaderSize];
            Buffer.BlockCopy(plaintextWithHeader, ExpirationTimeHeaderSize, retVal, 0, retVal.Length);
            expiration = embeddedExpiration;
            return retVal;
        }
        catch (Exception ex) when (ex.RequiresHomogenization())
        {
            // Homogenize all failures to CryptographicException
            throw new CryptographicException(Resources.CryptCommon_GenericError, ex);
        }
    }

#if NET
    // The span-based paths produce and consume exactly the same payload as the byte[]-based paths below, but avoid
    // allocating the intermediate { expiration || plaintext } array. The pooled buffers hold plaintext, so they are
    // cleared before being returned to the pool and never escape this call.
    private static byte[] ProtectWithSpanProtector(ISpanDataProtector protector, byte[] plaintext, DateTimeOffset expiration)
    {
        var plaintextWithHeaderLength = checked(ExpirationTimeHeaderSize + plaintext.Length);
        var plaintextWithHeader = ArrayPool<byte>.Shared.Rent(plaintextWithHeaderLength);
        var destination = new ClearingPooledBufferWriter(plaintextWithHeaderLength + ProtectOverheadEstimate);
        try
        {
            // We prepend the expiration time (as a 64-bit UTC tick count) to the unprotected data.
            BinaryPrimitives.WriteUInt64BigEndian(plaintextWithHeader, (ulong)expiration.UtcTicks);
            plaintext.CopyTo(plaintextWithHeader.AsSpan(ExpirationTimeHeaderSize));

            protector.Protect(plaintextWithHeader.AsSpan(0, plaintextWithHeaderLength), ref destination);
            return destination.WrittenSpan.ToArray();
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(plaintextWithHeader, clearArray: true);
            destination.Dispose();
        }
    }

    private static byte[] UnprotectWithSpanProtector(ISpanDataProtector protector, byte[] protectedData, DateTimeOffset now, out DateTimeOffset expiration)
    {
        // The plaintext is never larger than the protected payload for the built-in protectors; the writer grows if needed.
        var destination = new ClearingPooledBufferWriter(protectedData.Length);
        try
        {
            protector.Unprotect(protectedData, ref destination);
            var plaintextWithHeader = destination.WrittenSpan;
            if (plaintextWithHeader.Length < ExpirationTimeHeaderSize)
            {
                // header isn't present
                throw new CryptographicException(Resources.TimeLimitedDataProtector_PayloadInvalid);
            }

            // Read expiration time back out of the payload
            ulong utcTicksExpiration = BinaryPrimitives.ReadUInt64BigEndian(plaintextWithHeader);
            DateTimeOffset embeddedExpiration = new DateTimeOffset(checked((long)utcTicksExpiration), TimeSpan.Zero /* UTC */);

            // Are we expired?
            if (now > embeddedExpiration)
            {
                throw new CryptographicException(Resources.FormatTimeLimitedDataProtector_PayloadExpired(embeddedExpiration));
            }

            // Not expired - return payload
            expiration = embeddedExpiration;
            return plaintextWithHeader.Slice(ExpirationTimeHeaderSize).ToArray();
        }
        finally
        {
            destination.Dispose();
        }
    }

    // Magic header + key id + key modifier + IV/nonce + padding + MAC/tag for the built-in encryptors.
    private const int ProtectOverheadEstimate = 128;
#endif

    /*
     * EXPLICIT INTERFACE IMPLEMENTATIONS
     */

    IDataProtector IDataProtectionProvider.CreateProtector(string purpose)
    {
        ArgumentNullThrowHelper.ThrowIfNull(purpose);

        return CreateProtector(purpose);
    }

    byte[] IDataProtector.Protect(byte[] plaintext)
    {
        ArgumentNullThrowHelper.ThrowIfNull(plaintext);

        // MaxValue essentially means 'no expiration'
        return Protect(plaintext, DateTimeOffset.MaxValue);
    }

    byte[] IDataProtector.Unprotect(byte[] protectedData)
    {
        ArgumentNullThrowHelper.ThrowIfNull(protectedData);

        return Unprotect(protectedData, out _);
    }
}
