// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.AspNetCore.DataProtection.AuthenticatedEncryption;

/// <summary>
/// The basic interface for providing an authenticated encryption and decryption routine.
/// </summary>
public interface IAuthenticatedEncryptor
{
    /// <summary>
    /// Validates the authentication tag of and decrypts a blob of encrypted data.
    /// </summary>
    /// <param name="ciphertext">The ciphertext (including authentication tag) to decrypt.</param>
    /// <param name="additionalAuthenticatedData">Any ancillary data which was used during computation
    /// of the authentication tag. The same AAD must have been specified in the corresponding
    /// call to 'Encrypt'.</param>
    /// <returns>The original plaintext data (if the authentication tag was validated and decryption succeeded).</returns>
    /// <remarks>All cryptography-related exceptions should be homogenized to CryptographicException.</remarks>
    byte[] Decrypt(ArraySegment<byte> ciphertext, ArraySegment<byte> additionalAuthenticatedData);

    /// <summary>
    /// Encrypts and tamper-proofs a piece of data.
    /// </summary>
    /// <param name="plaintext">The plaintext to encrypt. This input may be zero bytes in length.</param>
    /// <param name="additionalAuthenticatedData">A piece of data which will not be included in
    /// the returned ciphertext but which will still be covered by the authentication tag.
    /// This input may be zero bytes in length. The same AAD must be specified in the corresponding
    /// call to Decrypt.</param>
    /// <returns>The ciphertext blob, including authentication tag.</returns>
    /// <remarks>All cryptography-related exceptions should be homogenized to CryptographicException.</remarks>
    byte[] Encrypt(ArraySegment<byte> plaintext, ArraySegment<byte> additionalAuthenticatedData);
}
