// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Security.Cryptography;

namespace Microsoft.AspNetCore.DataProtection
{
    /// <summary>
    /// An interface that can provide data protection services.
    /// </summary>
    public interface IDataProtector : IDataProtectionProvider
    {
        /// <summary>
        /// Cryptographically protects a piece of plaintext data.
        /// </summary>
        /// <param name="plaintext">The plaintext data to protect.</param>
        /// <returns>The protected form of the plaintext data.</returns>
        byte[] Protect(byte[] plaintext);

        /// <summary>
        /// Cryptographically unprotects a piece of protected data.
        /// </summary>
        /// <param name="protectedData">The protected data to unprotect.</param>
        /// <returns>The plaintext form of the protected data.</returns>
        /// <exception cref="System.Security.Cryptography.CryptographicException">
        /// Thrown if the protected data is invalid or malformed.
        /// </exception>
        byte[] Unprotect(byte[] protectedData);


#if NETCOREAPP
        /// <summary>
        /// Cryptographically protects a piece of plaintext data.
        /// </summary>
        /// <param name="output">Where to write the protected form of the plaintext data.</param>
        /// <param name="plaintext">The plaintext data to protect.</param>
        /// <param name="bytesWritten">The number of bytes written to output.</param>
        /// <returns>true if output is long enough to receive the protected data; otherwise, false.</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("ApiDesign", "RS0016:Add public types and members to the declared API", Justification = "<Pending>")]
        bool TryProtect(Span<byte> output, ReadOnlySpan<byte> plaintext, out int bytesWritten)
            => throw new NotImplementedException();

        /// <summary>
        /// Cryptographically unprotects a piece of protected data.
        /// </summary>
        /// <param name="output">Where the plaintext form of the protected data will be written.</param>
        /// <param name="protectedData">The protected data to unprotect.</param>
        /// <param name="bytesWritten">The number of bytes written to output.</param>
        /// <returns>true if plaintextOutput is long enough to receive the plaintext data; otherwise, false.</returns>
        /// <exception cref="CryptographicException">
        /// Thrown if the protected data is invalid or malformed.
        /// </exception>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("ApiDesign", "RS0016:Add public types and members to the declared API", Justification = "<Pending>")]
        bool TryUnprotect(Span<byte> output, ReadOnlySpan<byte> protectedData, out int bytesWritten)
            => throw new NotImplementedException();
#endif

    }
}
