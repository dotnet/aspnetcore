// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.DataProtection;

#if NET10_0_OR_GREATER

/// <summary>
/// An interface that can provide data protection services.
/// Is an optimized version of <see cref="IDataProtector"/>.
/// </summary>
public interface IOptimizedDataProtector : IDataProtector
{
    /// <summary>
    /// Returns the size of the encrypted data for a given plaintext length.
    /// </summary>
    /// <param name="plainText">The plain text that will be encrypted later</param>
    /// <returns>The length of the encrypted data</returns>
    int GetProtectedSize(ReadOnlySpan<byte> plainText);

    /// <summary>
    /// Attempts to encrypt and tamper-proof a piece of data.
    /// </summary>
    /// <param name="plainText">The input to encrypt.</param>
    /// <param name="destination">The ciphertext blob, including authentication tag.</param>
    /// <param name="bytesWritten">When this method returns, the total number of bytes written into destination</param>
    /// <returns>true if destination is long enough to receive the encrypted data; otherwise, false.</returns>
    bool TryProtect(ReadOnlySpan<byte> plainText, Span<byte> destination, out int bytesWritten);
}

#endif
