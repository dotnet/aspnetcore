// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.DataProtection;

/// <summary>
/// An interface that can provide data protection services.
/// Is an optimized version of <see cref="IDataProtector"/>.
/// </summary>
public interface ISpanDataProtector : IDataProtector
{
    /// <summary>
    /// Determines the size of the protected data in order to then use <see cref="TryProtect(ReadOnlySpan{byte}, Span{byte}, out int)"/>."/>.
    /// </summary>
    /// <param name="plainTextLength">The plain text length which will be encrypted later.</param>
    /// <returns>The size of the protected data.</returns>
    int GetProtectedSize(int plainTextLength);

    /// <summary>
    /// Returns the size of the decrypted data for a given ciphertext length.
    /// Size can be an over-estimation, the specific size will be written in <i>bytesWritten</i> parameter of the <see cref="TryUnprotect"/>
    /// </summary>
    /// <param name="cipherTextLength">Length of the cipher text that will be decrypted later.</param>
    /// <returns>The length of the decrypted data.</returns>
    int GetUnprotectedSize(int cipherTextLength);

    /// <summary>
    /// Attempts to encrypt and tamper-proof a piece of data.
    /// </summary>
    /// <param name="plainText">The input to encrypt.</param>
    /// <param name="destination">The ciphertext blob, including authentication tag.</param>
    /// <param name="bytesWritten">When this method returns, the total number of bytes written into destination</param>
    /// <returns>true if destination is long enough to receive the encrypted data; otherwise, false.</returns>
    bool TryProtect(ReadOnlySpan<byte> plainText, Span<byte> destination, out int bytesWritten);

    /// <summary>
    /// Attempts to validate the authentication tag of and decrypt a blob of encrypted data.
    /// </summary>
    /// <param name="cipherText">The encrypted data to decrypt.</param>
    /// <param name="destination">The decrypted output.</param>
    /// <param name="bytesWritten">When this method returns, the total number of bytes written into destination</param>
    /// <returns>true if decryption was successful; otherwise, false.</returns>
    bool TryUnprotect(ReadOnlySpan<byte> cipherText, Span<byte> destination, out int bytesWritten);
}
