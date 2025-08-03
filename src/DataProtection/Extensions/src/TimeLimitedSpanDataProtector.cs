// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Buffers;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Security.Cryptography;
using System.Threading;
using Microsoft.AspNetCore.DataProtection.Extensions;
using Microsoft.AspNetCore.Shared;

namespace Microsoft.AspNetCore.DataProtection;

/// <summary>
/// Wraps an existing <see cref="IDataProtector"/> and appends a purpose that allows
/// protecting data with a finite lifetime.
/// </summary>
internal sealed class TimeLimitedSpanDataProtector : TimeLimitedDataProtector, ISpanDataProtector
{
    public TimeLimitedSpanDataProtector(ISpanDataProtector innerProtector) : base(innerProtector)
    {
    }

    public int GetProtectedSize(ReadOnlySpan<byte> plainText)
    {
        var dataProtector = (ISpanDataProtector)GetInnerProtectorWithTimeLimitedPurpose();
        return dataProtector.GetProtectedSize(plainText) + ExpirationTimeHeaderSize;
    }

    public bool TryProtect(ReadOnlySpan<byte> plaintext, Span<byte> destination, out int bytesWritten)
        => TryProtect(plaintext, destination, DateTimeOffset.MaxValue, out bytesWritten);

    public bool TryProtect(ReadOnlySpan<byte> plaintext, Span<byte> destination, DateTimeOffset expiration, out int bytesWritten)
    {
        var innerProtector = (ISpanDataProtector)_innerProtector;

        // we need to prepend the expiration time, so we need to allocate a buffer for the plaintext with header
        byte[]? plainTextWithHeader = null;
        try
        {
            plainTextWithHeader = ArrayPool<byte>.Shared.Rent(plaintext.Length + ExpirationTimeHeaderSize);
            var plainTextWithHeaderSpan = plainTextWithHeader.AsSpan(0, plaintext.Length + ExpirationTimeHeaderSize);

            // We prepend the expiration time (as a 64-bit UTC tick count) to the unprotected data.
            BitHelpers.WriteUInt64(plainTextWithHeaderSpan, 0, (ulong)expiration.UtcTicks);

            // and copy the plaintext into the buffer
            plaintext.CopyTo(plainTextWithHeaderSpan.Slice(ExpirationTimeHeaderSize));

            return innerProtector.TryProtect(plainTextWithHeaderSpan, destination, out bytesWritten);
        }
        finally
        {
            if (plainTextWithHeader is not null)
            {
                ArrayPool<byte>.Shared.Return(plainTextWithHeader);
            }
        }
    }
}
