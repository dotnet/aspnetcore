// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNetCore.DataProtection.AuthenticatedEncryption
{
    /// <summary>
    /// An optimized encryptor that can avoid buffer allocations in common code paths.
    /// </summary>
    internal interface IOptimizedAuthenticatedEncryptor : IAuthenticatedEncryptor
    {
        /// <summary>
        /// Encrypts and tamper-proofs a piece of data.
        /// </summary>
        /// <param name="plaintext">The plaintext to encrypt. This input may be zero bytes in length.</param>
        /// <param name="additionalAuthenticatedData">A piece of data which will not be included in
        /// the returned ciphertext but which will still be covered by the authentication tag.
        /// This input may be zero bytes in length. The same AAD must be specified in the corresponding
        /// call to Decrypt.</param>
        /// <param name="preBufferSize">The number of bytes to pad before the ciphertext in the output.</param>
        /// <param name="postBufferSize">The number of bytes to pad after the ciphertext in the output.</param>
        /// <returns>
        /// The ciphertext blob, including authentication tag. The ciphertext blob will be surrounded by
        /// the number of padding bytes requested. For instance, if the given (plaintext, AAD) input results
        /// in a (ciphertext, auth tag) output of 0x0102030405, and if 'preBufferSize' is 3 and
        /// 'postBufferSize' is 5, then the return value will be 0xYYYYYY0102030405ZZZZZZZZZZ, where bytes
        /// YY and ZZ are undefined.
        /// </returns>
        /// <remarks>
        /// This method allows for a slight performance improvement over IAuthenticatedEncryptor.Encrypt
        /// in the case where the caller needs to prepend or append some data to the resulting ciphertext.
        /// For instance, if the caller needs to append a 32-bit header to the resulting ciphertext, then
        /// specify 4 for 'preBufferSize' and overwrite the first 32 bits of the buffer returned
        /// by this function. This saves the caller from having to allocate a new buffer to hold the final
        /// transformed result.
        ///
        /// All cryptography-related exceptions should be homogenized to CryptographicException.
        /// </remarks>
        byte[] Encrypt(ArraySegment<byte> plaintext, ArraySegment<byte> additionalAuthenticatedData, uint preBufferSize, uint postBufferSize);
    }
}
