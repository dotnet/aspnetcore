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
    /// <br/> Returns the boolean representing if current implementation of data protector supports <see cref="ISpanDataProtector"/> or not.
    /// If it does not (returns false), then one needs to fallback to <see cref="IDataProtector"/> and use <see cref="IDataProtector.Protect(byte[])"/> and <see cref="IDataProtector.Unprotect(byte[])"/> methods instead.
    /// </summary>
    /// <param name="plainText">The plain text that will be encrypted later</param>
    /// <param name="cipherTextLength">The length of the expected cipher text.</param>
    /// <returns>true, if <see cref="ISpanDataProtector"/> is supported. False if a fallback to <see cref="IDataProtector"/> is required.</returns>
    bool TryGetProtectedSize(ReadOnlySpan<byte> plainText, out int cipherTextLength);

    /// <summary>
    /// Attempts to encrypt and tamper-proof a piece of data.
    /// </summary>
    /// <param name="plainText">The input to encrypt.</param>
    /// <param name="destination">The ciphertext blob, including authentication tag.</param>
    /// <param name="bytesWritten">When this method returns, the total number of bytes written into destination</param>
    /// <returns>true if destination is long enough to receive the encrypted data; otherwise, false.</returns>
    bool TryProtect(ReadOnlySpan<byte> plainText, Span<byte> destination, out int bytesWritten);
}
