// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

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
    }
}
