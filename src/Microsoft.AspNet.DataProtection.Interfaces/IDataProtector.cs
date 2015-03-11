// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Security.Cryptography;
using Microsoft.Framework.Internal;

namespace Microsoft.AspNet.DataProtection
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
        byte[] Protect([NotNull] byte[] plaintext);

        /// <summary>
        /// Cryptographically unprotects a piece of protected data.
        /// </summary>
        /// <param name="protectedData">The protected data to unprotect.</param>
        /// <returns>The plaintext form of the protected data.</returns>
        /// <exception cref="CryptographicException">
        /// Thrown if the protected data is invalid or malformed.
        /// </exception>
        byte[] Unprotect([NotNull] byte[] protectedData);
    }
}
