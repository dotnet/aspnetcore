// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNet.Security.DataProtection.AuthenticatedEncryption
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
        /// <param name="preBufferSize">The number of bytes to include before the ciphertext in the return value.</param>
        /// <param name="postBufferSize">The number of bytes to include after the ciphertext in the return value.</param>
        /// <returns>
        /// A buffer containing the ciphertext and authentication tag.
        /// If a non-zero pre-buffer or post-buffer size is specified, the returned buffer will contain appropriate padding
        /// on either side of the ciphertext and authentication tag. For instance, if a pre-buffer size of 4 and a post-buffer
        /// size of 7 are specified, and if the ciphertext and tag are a combined 48 bytes, then the returned buffer will
        /// be a total 59 bytes in length. The first four bytes will be undefined, the next 48 bytes will contain the
        /// ciphertext and tag, and the last seven bytes will be undefined. The intent is that the caller can overwrite the
        /// pre-buffer or post-buffer with a header or footer without needing to allocate an additional buffer object.
        /// </returns>
        /// <remarks>All cryptography-related exceptions should be homogenized to CryptographicException.</remarks>
        byte[] Encrypt(ArraySegment<byte> plaintext, ArraySegment<byte> additionalAuthenticatedData, uint preBufferSize, uint postBufferSize);
    }
}
