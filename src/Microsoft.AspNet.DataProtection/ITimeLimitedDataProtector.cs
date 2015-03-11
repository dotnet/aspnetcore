// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNet.DataProtection
{
    /// <summary>
    /// An interface that can provide data protection services.
    /// </summary>
    public interface ITimeLimitedDataProtector : IDataProtector
    {
        /// <summary>
        /// Creates an IDataProtector given a purpose.
        /// </summary>
        /// <param name="purposes">
        /// The purpose to be assigned to the newly-created IDataProtector.
        /// This parameter must be unique for the intended use case; two different IDataProtector
        /// instances created with two different 'purpose' strings will not be able
        /// to understand each other's payloads. The 'purpose' parameter is not intended to be
        /// kept secret.
        /// </param>
        /// <returns>An IDataProtector tied to the provided purpose.</returns>
        new ITimeLimitedDataProtector CreateProtector(string purpose);

        /// <summary>
        /// Cryptographically protects a piece of plaintext data and assigns an expiration date to the data.
        /// </summary>
        /// <param name="plaintext">The plaintext data to protect.</param>
        /// <param name="expiration">The date after which the data can no longer be unprotected.</param>
        /// <returns>The protected form of the plaintext data.</returns>
        byte[] Protect(byte[] plaintext, DateTimeOffset expiration);

        /// <summary>
        /// Cryptographically unprotects a piece of protected data.
        /// </summary>
        /// <param name="protectedData">The protected data to unprotect.</param>
        /// <param name="expiration">After unprotection, contains the expiration date of the protected data.</param>
        /// <returns>The plaintext form of the protected data.</returns>
        /// <remarks>
        /// Implementations should throw CryptographicException if the protected data is invalid or malformed.
        /// </remarks>
        byte[] Unprotect(byte[] protectedData, out DateTimeOffset expiration);
    }
}
