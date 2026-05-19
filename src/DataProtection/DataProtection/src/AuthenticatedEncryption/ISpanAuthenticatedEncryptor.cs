// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#if NET

using System;
using System.Buffers;
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
    /// Encrypts and authenticates a piece of plaintext data and writes the result to a buffer writer.
    /// </summary>
    /// <typeparam name="TWriter">The type of buffer writer to write the ciphertext to.</typeparam>
    /// <param name="plaintext">The plaintext to encrypt. This input may be zero bytes in length.</param>
    /// <param name="additionalAuthenticatedData">
    /// A piece of data which will not be included in the returned ciphertext
    /// but which will still be covered by the authentication tag. This input may be zero bytes in length.
    /// The same AAD must be specified in the corresponding call to <see cref="Decrypt{TWriter}"/>.
    /// </param>
    /// <param name="destination">The buffer writer to which the ciphertext (including authentication tag) will be written.</param>
    /// <remarks>
    /// This method provides an optimized, streaming alternative to <see cref="IAuthenticatedEncryptor.Encrypt(System.ArraySegment{byte}, System.ArraySegment{byte})"/>.
    /// Rather than allocating an intermediate buffer, the ciphertext is written directly to the provided buffer writer,
    /// which can improve performance and reduce memory allocation pressure.
    /// The buffer writer is advanced by the total number of bytes written to it.
    /// </remarks>
    void Encrypt<TWriter>(ReadOnlySpan<byte> plaintext, ReadOnlySpan<byte> additionalAuthenticatedData, ref TWriter destination)
        where TWriter : IBufferWriter<byte>, allows ref struct;

    /// <summary>
    /// Validates the authentication tag of and decrypts a blob of encrypted data, writing the result to a buffer writer.
    /// </summary>
    /// <typeparam name="TWriter">The type of buffer writer to write the plaintext to.</typeparam>
    /// <param name="ciphertext">The ciphertext (including authentication tag) to decrypt.</param>
    /// <param name="additionalAuthenticatedData">
    /// Any ancillary data which was used during computation of the authentication tag.
    /// The same AAD must have been specified in the corresponding call to <see cref="Encrypt{TWriter}"/>.
    /// </param>
    /// <param name="destination">The buffer writer to which the decrypted plaintext will be written.</param>
    /// <remarks>
    /// This method provides an optimized, streaming alternative to <see cref="IAuthenticatedEncryptor.Decrypt(System.ArraySegment{byte}, System.ArraySegment{byte})"/>.
    /// Rather than allocating an intermediate buffer, the plaintext is written directly to the provided buffer writer,
    /// which can improve performance and reduce memory allocation pressure.
    /// The buffer writer is advanced by the total number of bytes written to it.
    /// </remarks>
    void Decrypt<TWriter>(ReadOnlySpan<byte> ciphertext, ReadOnlySpan<byte> additionalAuthenticatedData, ref TWriter destination)
        where TWriter : IBufferWriter<byte>, allows ref struct;
}

#endif
