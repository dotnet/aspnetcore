// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
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

        // We prepend the expiration time (as a 64-bit UTC tick count) to the unprotected data.
        byte[] plaintextWithHeader = new byte[checked(8 + plaintext.Length)];
        BitHelpers.WriteUInt64(plaintextWithHeader, 0, (ulong)expiration.UtcTicks);
        Buffer.BlockCopy(plaintext, 0, plaintextWithHeader, 8, plaintext.Length);

        return GetInnerProtectorWithTimeLimitedPurpose().Protect(plaintextWithHeader);
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
            byte[] plaintextWithHeader = GetInnerProtectorWithTimeLimitedPurpose().Unprotect(protectedData);
            if (plaintextWithHeader.Length < 8)
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
            byte[] retVal = new byte[plaintextWithHeader.Length - 8];
            Buffer.BlockCopy(plaintextWithHeader, 8, retVal, 0, retVal.Length);
            expiration = embeddedExpiration;
            return retVal;
        }
        catch (Exception ex) when (ex.RequiresHomogenization())
        {
            // Homogenize all failures to CryptographicException
            throw new CryptographicException(Resources.CryptCommon_GenericError, ex);
        }
    }

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
