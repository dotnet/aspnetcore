// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#if NET

using System;
using System.Buffers;
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
    /// Cryptographically protects a piece of plaintext data and writes the result to a buffer writer.
    /// </summary>
    /// <typeparam name="TWriter">The type of buffer writer to write the protected data to.</typeparam>
    /// <param name="plaintext">The plaintext data to protect.</param>
    /// <param name="destination">The buffer writer to which the protected data will be written.</param>
    /// <remarks>
    /// This method provides an optimized, streaming alternative to <see cref="IDataProtector.Protect(byte[])"/>.
    /// Rather than allocating an intermediate buffer, the protected data is written directly to the provided
    /// buffer writer, which can improve performance and reduce memory allocation pressure.
    /// The buffer writer is advanced by the total number of bytes written to it.
    /// </remarks>
    void Protect<TWriter>(ReadOnlySpan<byte> plaintext, ref TWriter destination)
        where TWriter : IBufferWriter<byte>, allows ref struct;

    /// <summary>
    /// Cryptographically unprotects a piece of protected data and writes the result to a buffer writer.
    /// </summary>
    /// <typeparam name="TWriter">The type of buffer writer to write the unprotected data to.</typeparam>
    /// <param name="protectedData">The protected data to unprotect.</param>
    /// <param name="destination">The buffer writer to which the unprotected plaintext will be written.</param>
    /// <remarks>
    /// This method provides an optimized, streaming alternative to <see cref="IDataProtector.Unprotect(byte[])"/>.
    /// Rather than allocating an intermediate buffer, the unprotected plaintext is written directly to the provided
    /// buffer writer, which can improve performance and reduce memory allocation pressure.
    /// The buffer writer is advanced by the total number of bytes written to it.
    /// </remarks>
    void Unprotect<TWriter>(ReadOnlySpan<byte> protectedData, ref TWriter destination)
        where TWriter : IBufferWriter<byte>, allows ref struct;
}

#endif
