// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Security.Cryptography;

namespace Microsoft.AspNet.Security.DataProtection
{
    /// <summary>
    /// Helpful extension methods for data protection APIs.
    /// </summary>
    public static class DataProtectionExtensions
    {
        /// <summary>
        /// Cryptographically protects a piece of plaintext data.
        /// </summary>
        /// <param name="protector">The data protector to use for this operation.</param>
        /// <param name="unprotectedData">The plaintext data to protect.</param>
        /// <returns>The protected form of the plaintext data.</returns>
        public static string Protect([NotNull] this IDataProtector protector, [NotNull] string unprotectedData)
        {
            try
            {
                byte[] unprotectedDataAsBytes = CryptoUtil.SecureUtf8Encoding.GetBytes(unprotectedData);
                byte[] protectedDataAsBytes = protector.Protect(unprotectedDataAsBytes);
                return WebEncoders.Base64UrlEncode(protectedDataAsBytes);
            }
            catch (Exception ex) if (!(ex is CryptographicException))
            {
                // Homogenize exceptions to CryptographicException
                throw Error.CryptCommon_GenericError(ex);
            }
        }

        /// <summary>
        /// Cryptographically unprotects a piece of protected data.
        /// </summary>
        /// <param name="protector">The data protector to use for this operation.</param>
        /// <param name="protectedData">The protected data to unprotect.</param>
        /// <returns>The plaintext form of the protected data.</returns>
        /// <remarks>
        /// This method will throw CryptographicException if the input is invalid or malformed.
        /// </remarks>
        public static string Unprotect([NotNull] this IDataProtector protector, [NotNull] string protectedData)
        {
            try
            {
                byte[] protectedDataAsBytes = WebEncoders.Base64UrlDecode(protectedData);
                byte[] unprotectedDataAsBytes = protector.Unprotect(protectedDataAsBytes);
                return CryptoUtil.SecureUtf8Encoding.GetString(unprotectedDataAsBytes);
            }
            catch (Exception ex) if (!(ex is CryptographicException))
            {
                // Homogenize exceptions to CryptographicException
                throw Error.CryptCommon_GenericError(ex);
            }
        }
    }
}
