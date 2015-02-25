// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using Microsoft.AspNet.Cryptography;

namespace Microsoft.AspNet.Security.DataProtection
{
    /// <summary>
    /// Helpful extension methods for data protection APIs.
    /// </summary>
    public static class DataProtectionExtensions
    {
        /// <summary>
        /// Creates a time-limited data protector based on an existing protector.
        /// </summary>
        /// <param name="protector">The existing protector from which to derive a time-limited protector.</param>
        /// <returns>A time-limited data protector.</returns>
        public static ITimeLimitedDataProtector AsTimeLimitedDataProtector([NotNull] this IDataProtector protector)
        {
            return (protector as ITimeLimitedDataProtector)
                ?? new TimeLimitedDataProtector(protector.CreateProtector(TimeLimitedDataProtector.PurposeString));
        }

        /// <summary>
        /// Creates an IDataProtector given an array of purposes.
        /// </summary>
        /// <param name="provider">The provider from which to generate the purpose chain.</param>
        /// <param name="purposes">
        /// This is a convenience method used for chaining several purposes together
        /// in a single call to CreateProtector. See the documentation of
        /// IDataProtectionProvider.CreateProtector for more information.
        /// </param>
        /// <returns>An IDataProtector tied to the provided purpose chain.</returns>
        public static IDataProtector CreateProtector([NotNull] this IDataProtectionProvider provider, params string[] purposes)
        {
            if (purposes == null || purposes.Length == 0)
            {
                throw new ArgumentException(Resources.DataProtectionExtensions_NullPurposesArray, nameof(purposes));
            }

            IDataProtectionProvider retVal = provider;
            foreach (string purpose in purposes)
            {
                if (String.IsNullOrEmpty(purpose))
                {
                    throw new ArgumentException(Resources.DataProtectionExtensions_NullPurposesArray, nameof(purposes));
                }
                retVal = retVal.CreateProtector(purpose) ?? CryptoUtil.Fail<IDataProtector>("CreateProtector returned null.");
            }

            Debug.Assert(retVal is IDataProtector); // CreateProtector is supposed to return an instance of this interface
            return (IDataProtector)retVal;
        }

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
                byte[] unprotectedDataAsBytes = EncodingUtil.SecureUtf8Encoding.GetBytes(unprotectedData);
                byte[] protectedDataAsBytes = protector.Protect(unprotectedDataAsBytes);
                return WebEncoders.Base64UrlEncode(protectedDataAsBytes);
            }
            catch (Exception ex) when (ex.RequiresHomogenization())
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
                return EncodingUtil.SecureUtf8Encoding.GetString(unprotectedDataAsBytes);
            }
            catch (Exception ex) when (ex.RequiresHomogenization())
            {
                // Homogenize exceptions to CryptographicException
                throw Error.CryptCommon_GenericError(ex);
            }
        }
    }
}
