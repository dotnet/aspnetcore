// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNetCore.DataProtection.AuthenticatedEncryption
{
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

#if NETCOREAPP
        /// <summary>
        /// Returns true if this implementation supports the span enabled versions of TryEncrypt/Decrypt
        /// </summary>
        /// <returns>true if this implementation supports the span enabled versions of TryEncrypt/Decrypt.</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("ApiDesign", "RS0016:Add public types and members to the declared API", Justification = "<Pending>")]
        bool IsSpanEnabled() => false;

        /// <summary>
        /// Encrypts and tamper-proofs a piece of data.
        /// </summary>
        /// <param name="output">Where the ciphertext blob, including authentication tag will be written.</param>
        /// <param name="plaintext">The plaintext to encrypt. This input may be zero bytes in length.</param>
        /// <param name="additionalAuthenticatedData">A piece of data which will not be included in
        /// the returned ciphertext but which will still be covered by the authentication tag.
        /// This input may be zero bytes in length. The same AAD must be specified in the corresponding
        /// call to Decrypt.</param>
        /// <param name="bytesWritten">The number of bytes written to output.</param>
        /// <returns>true if output is long enough to receive the protected data; otherwise, false.</returns>
        /// <remarks>All cryptography-related exceptions should be homogenized to CryptographicException.</remarks>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("ApiDesign", "RS0016:Add public types and members to the declared API", Justification = "<Pending>")]
        bool TryEncrypt(Span<byte> output, ReadOnlySpan<byte> plaintext, ReadOnlySpan<byte> additionalAuthenticatedData, out int bytesWritten)
            => throw new NotImplementedException();


        /// <summary>
        /// Validates the authentication tag of and decrypts a blob of encrypted data.
        /// </summary>
        /// <param name="output">Where the original plaintext data will be written (if the authentication tag was validated and decryption succeeded).</param>
        /// <param name="ciphertext">The ciphertext (including authentication tag) to decrypt.</param>
        /// <param name="additionalAuthenticatedData">Any ancillary data which was used during computation
        /// of the authentication tag. The same AAD must have been specified in the corresponding
        /// call to 'Encrypt'.</param>
        /// <param name="bytesWritten">The number of bytes written to output.</param>
        /// <returns>true if output is long enough to receive the protected data; otherwise, false.</returns>
        /// <remarks>All cryptography-related exceptions should be homogenized to CryptographicException.</remarks>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("ApiDesign", "RS0016:Add public types and members to the declared API", Justification = "<Pending>")]
        bool TryDecrypt(Span<byte> output, ReadOnlySpan<byte> ciphertext, ReadOnlySpan<byte> additionalAuthenticatedData, out int bytesWritten)
            => throw new NotImplementedException();
#endif
    }
}
