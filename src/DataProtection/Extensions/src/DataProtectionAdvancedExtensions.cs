// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Shared;

namespace Microsoft.AspNetCore.DataProtection;

/// <summary>
/// Helpful extension methods for data protection APIs.
/// </summary>
public static class DataProtectionAdvancedExtensions
{
    /// <summary>
    /// Cryptographically protects a piece of plaintext data, expiring the data after
    /// the specified amount of time has elapsed.
    /// </summary>
    /// <param name="protector">The protector to use.</param>
    /// <param name="plaintext">The plaintext data to protect.</param>
    /// <param name="lifetime">The amount of time after which the payload should no longer be unprotectable.</param>
    /// <returns>The protected form of the plaintext data.</returns>
    public static byte[] Protect(this ITimeLimitedDataProtector protector, byte[] plaintext, TimeSpan lifetime)
    {
        ArgumentNullThrowHelper.ThrowIfNull(protector);
        ArgumentNullThrowHelper.ThrowIfNull(plaintext);

        return protector.Protect(plaintext, DateTimeOffset.UtcNow + lifetime);
    }

    /// <summary>
    /// Cryptographically protects a piece of plaintext data, expiring the data at
    /// the chosen time.
    /// </summary>
    /// <param name="protector">The protector to use.</param>
    /// <param name="plaintext">The plaintext data to protect.</param>
    /// <param name="expiration">The time when this payload should expire.</param>
    /// <returns>The protected form of the plaintext data.</returns>
    public static string Protect(this ITimeLimitedDataProtector protector, string plaintext, DateTimeOffset expiration)
    {
        ArgumentNullThrowHelper.ThrowIfNull(protector);
        ArgumentNullThrowHelper.ThrowIfNull(plaintext);

        var wrappingProtector = new TimeLimitedWrappingProtector(protector) { Expiration = expiration };
        return wrappingProtector.Protect(plaintext);
    }

    /// <summary>
    /// Cryptographically protects a piece of plaintext data, expiring the data after
    /// the specified amount of time has elapsed.
    /// </summary>
    /// <param name="protector">The protector to use.</param>
    /// <param name="plaintext">The plaintext data to protect.</param>
    /// <param name="lifetime">The amount of time after which the payload should no longer be unprotectable.</param>
    /// <returns>The protected form of the plaintext data.</returns>
    public static string Protect(this ITimeLimitedDataProtector protector, string plaintext, TimeSpan lifetime)
    {
        ArgumentNullThrowHelper.ThrowIfNull(protector);
        ArgumentNullThrowHelper.ThrowIfNull(plaintext);

        return Protect(protector, plaintext, DateTimeOffset.Now + lifetime);
    }

    /// <summary>
    /// Converts an <see cref="IDataProtector"/> into an <see cref="ITimeLimitedDataProtector"/>
    /// so that payloads can be protected with a finite lifetime.
    /// </summary>
    /// <param name="protector">The <see cref="IDataProtector"/> to convert to a time-limited protector.</param>
    /// <returns>An <see cref="ITimeLimitedDataProtector"/>.</returns>
    public static ITimeLimitedDataProtector ToTimeLimitedDataProtector(this IDataProtector protector)
    {
        ArgumentNullThrowHelper.ThrowIfNull(protector);

        return (protector as ITimeLimitedDataProtector) ?? new TimeLimitedDataProtector(protector);
    }

    /// <summary>
    /// Cryptographically unprotects a piece of protected data.
    /// </summary>
    /// <param name="protector">The protector to use.</param>
    /// <param name="protectedData">The protected data to unprotect.</param>
    /// <param name="expiration">An 'out' parameter which upon a successful unprotect
    /// operation receives the expiration date of the payload.</param>
    /// <returns>The plaintext form of the protected data.</returns>
    /// <exception cref="System.Security.Cryptography.CryptographicException">
    /// Thrown if <paramref name="protectedData"/> is invalid, malformed, or expired.
    /// </exception>
    public static string Unprotect(this ITimeLimitedDataProtector protector, string protectedData, out DateTimeOffset expiration)
    {
        ArgumentNullThrowHelper.ThrowIfNull(protector);
        ArgumentNullThrowHelper.ThrowIfNull(protectedData);

        var wrappingProtector = new TimeLimitedWrappingProtector(protector);
        string retVal = wrappingProtector.Unprotect(protectedData);
        expiration = wrappingProtector.Expiration;
        return retVal;
    }

    private sealed class TimeLimitedWrappingProtector : IDataProtector
    {
        public DateTimeOffset Expiration;
        private readonly ITimeLimitedDataProtector _innerProtector;

        public TimeLimitedWrappingProtector(ITimeLimitedDataProtector innerProtector)
        {
            _innerProtector = innerProtector;
        }

        public IDataProtector CreateProtector(string purpose)
        {
            ArgumentNullThrowHelper.ThrowIfNull(purpose);

            throw new NotImplementedException();
        }

        public byte[] Protect(byte[] plaintext)
        {
            ArgumentNullThrowHelper.ThrowIfNull(plaintext);

            return _innerProtector.Protect(plaintext, Expiration);
        }

        public byte[] Unprotect(byte[] protectedData)
        {
            ArgumentNullThrowHelper.ThrowIfNull(protectedData);

            return _innerProtector.Unprotect(protectedData, out Expiration);
        }
    }
}
