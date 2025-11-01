// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.DataProtection.AuthenticatedEncryption;

/// <summary>
/// Provides an authenticated encryption and decryption routine via a span-based API.
/// </summary>
public interface ISpanAuthenticatedEncryptor : IAuthenticatedEncryptor
{
    /// <summary>
    /// Returns the size of the encrypted data for a given plaintext length.
    /// </summary>
    /// <param name="plainTextLength">Length of the plain text that will be encrypted later</param>
    /// <returns>The length of the encrypted data</returns>
    int GetEncryptedSize(int plainTextLength);

    /// <summary>
    /// Returns the size of the decrypted data for a given ciphertext length.
    /// Size can be an over-estimation, the specific size will be written in <i>bytesWritten</i> parameter of the <see cref="TryDecrypt"/>
    /// </summary>
    /// <param name="cipherTextLength">Length of the cipher text that will be decrypted later.</param>
    /// <returns>The length of the decrypted data.</returns>
    int GetDecryptedSize(int cipherTextLength);

    /// <summary>
    /// Attempts to encrypt and tamper-proof a piece of data.
    /// </summary>
    /// <param name="plaintext">The input to encrypt.</param>
    /// <param name="additionalAuthenticatedData">
    /// A piece of data which will not be included in
    /// the returned ciphertext but which will still be covered by the authentication tag.
    /// This input may be zero bytes in length. The same AAD must be specified in the corresponding decryption call.
    /// </param>
    /// <param name="destination">The ciphertext blob, including authentication tag.</param>
    /// <param name="bytesWritten">When this method returns, the total number of bytes written into destination</param>
    /// <returns>true if destination is long enough to receive the encrypted data; otherwise, false.</returns>
    bool TryEncrypt(ReadOnlySpan<byte> plaintext, ReadOnlySpan<byte> additionalAuthenticatedData, Span<byte> destination, out int bytesWritten);

    /// <summary>
    /// Attempts to validate the authentication tag of and decrypt a blob of encrypted data.
    /// </summary>
    /// <param name="cipherText">The encrypted data to decrypt.</param>
    /// <param name="additionalAuthenticatedData">
    /// A piece of data which was included in the authentication tag during encryption.
    /// This input may be zero bytes in length. The same AAD must be specified in the corresponding encryption call.
    /// </param>
    /// <param name="destination">The decrypted output.</param>
    /// <param name="bytesWritten">When this method returns, the total number of bytes written into destination</param>
    /// <returns>true if decryption was successful; otherwise, false.</returns>
    bool TryDecrypt(ReadOnlySpan<byte> cipherText, ReadOnlySpan<byte> additionalAuthenticatedData, Span<byte> destination, out int bytesWritten);
}
