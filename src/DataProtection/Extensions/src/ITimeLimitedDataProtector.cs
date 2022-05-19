// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;

namespace Microsoft.AspNetCore.DataProtection;

/// <summary>
/// An interface that can provide data protection services where payloads have
/// a finite lifetime.
/// </summary>
/// <remarks>
/// It is intended that payload lifetimes be somewhat short. Payloads protected
/// via this mechanism are not intended for long-term persistence (e.g., longer
/// than a few weeks).
/// </remarks>
public interface ITimeLimitedDataProtector : IDataProtector
{
    /// <summary>
    /// Creates an <see cref="ITimeLimitedDataProtector"/> given a purpose.
    /// </summary>
    /// <param name="purpose">
    /// The purpose to be assigned to the newly-created <see cref="ITimeLimitedDataProtector"/>.
    /// </param>
    /// <returns>An <see cref="ITimeLimitedDataProtector"/> tied to the provided purpose.</returns>
    /// <remarks>
    /// The <paramref name="purpose"/> parameter must be unique for the intended use case; two
    /// different <see cref="ITimeLimitedDataProtector"/> instances created with two different <paramref name="purpose"/>
    /// values will not be able to decipher each other's payloads. The <paramref name="purpose"/> parameter
    /// value is not intended to be kept secret.
    /// </remarks>
    new ITimeLimitedDataProtector CreateProtector(string purpose);

    /// <summary>
    /// Cryptographically protects a piece of plaintext data, expiring the data at
    /// the chosen time.
    /// </summary>
    /// <param name="plaintext">The plaintext data to protect.</param>
    /// <param name="expiration">The time when this payload should expire.</param>
    /// <returns>The protected form of the plaintext data.</returns>
    byte[] Protect(byte[] plaintext, DateTimeOffset expiration);

    /// <summary>
    /// Cryptographically unprotects a piece of protected data.
    /// </summary>
    /// <param name="protectedData">The protected data to unprotect.</param>
    /// <param name="expiration">An 'out' parameter which upon a successful unprotect
    /// operation receives the expiration date of the payload.</param>
    /// <returns>The plaintext form of the protected data.</returns>
    /// <exception cref="System.Security.Cryptography.CryptographicException">
    /// Thrown if <paramref name="protectedData"/> is invalid, malformed, or expired.
    /// </exception>
    byte[] Unprotect(byte[] protectedData, out DateTimeOffset expiration);
}
