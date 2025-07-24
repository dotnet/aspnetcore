// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;

namespace Microsoft.AspNetCore.DataProtection;

/// <summary>
/// An interface that can provide data protection services.
/// </summary>
public interface IDataProtector : IDataProtectionProvider
{
    /// <summary>
    /// Cryptographically protects a piece of plaintext data.
    /// </summary>
    /// <param name="plaintext">The plaintext data to protect.</param>
    /// <returns>The protected form of the plaintext data.</returns>
    byte[] Protect(byte[] plaintext);

    /// <summary>
    /// Cryptographically unprotects a piece of protected data.
    /// </summary>
    /// <param name="protectedData">The protected data to unprotect.</param>
    /// <returns>The plaintext form of the protected data.</returns>
    /// <exception cref="System.Security.Cryptography.CryptographicException">
    /// Thrown if the protected data is invalid or malformed.
    /// </exception>
    byte[] Unprotect(byte[] protectedData);

#if NET10_0_OR_GREATER
    /// <summary>
    /// Returns the size of the encrypted data for a given plaintext length.
    /// </summary>
    /// <param name="plainText">The plain text that will be encrypted later</param>
    /// <returns>The length of the encrypted data</returns>
    internal int GetProtectedSize(ReadOnlySpan<byte> plainText);

    /// <summary>
    /// Attempts to encrypt and tamper-proof a piece of data.
    /// </summary>
    /// <param name="plainText">The input to encrypt.</param>
    /// <param name="destination">The ciphertext blob, including authentication tag.</param>
    /// <param name="bytesWritten">When this method returns, the total number of bytes written into destination</param>
    /// <returns>true if destination is long enough to receive the encrypted data; otherwise, false.</returns>
    internal bool TryProtect(ReadOnlySpan<byte> plainText, Span<byte> destination, out int bytesWritten);
#endif
}
