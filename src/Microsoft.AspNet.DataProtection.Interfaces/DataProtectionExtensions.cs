// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.AspNet.DataProtection.Interfaces;
using Microsoft.Framework.Internal;

namespace Microsoft.AspNet.DataProtection
{
    /// <summary>
    /// Helpful extension methods for data protection APIs.
    /// </summary>
    public static class DataProtectionExtensions
    {
        /// <summary>
        /// Creates an <see cref="IDataProtector"/> given a list of purposes.
        /// </summary>
        /// <param name="provider">The <see cref="IDataProtectionProvider"/> from which to generate the purpose chain.</param>
        /// <param name="purposes">The list of purposes which contribute to the purpose chain. This list must
        /// contain at least one element, and it may not contain null elements.</param>
        /// <returns>An <see cref="IDataProtector"/> tied to the provided purpose chain.</returns>
        /// <remarks>
        /// This is a convenience method which chains together several calls to
        /// <see cref="IDataProtectionProvider.CreateProtector(string)"/>. See that method's
        /// documentation for more information.
        /// </remarks>
        public static IDataProtector CreateProtector([NotNull] this IDataProtectionProvider provider, [NotNull] IEnumerable<string> purposes)
        {
            bool collectionIsEmpty = true;
            IDataProtectionProvider retVal = provider;
            foreach (string purpose in purposes)
            {
                if (purpose == null)
                {
                    throw new ArgumentException(Resources.DataProtectionExtensions_NullPurposesCollection, nameof(purposes));
                }
                retVal = retVal.CreateProtector(purpose) ?? CryptoUtil.Fail<IDataProtector>("CreateProtector returned null.");
                collectionIsEmpty = false;
            }

            if (collectionIsEmpty)
            {
                throw new ArgumentException(Resources.DataProtectionExtensions_NullPurposesCollection, nameof(purposes));
            }

            Debug.Assert(retVal is IDataProtector); // CreateProtector is supposed to return an instance of this interface
            return (IDataProtector)retVal;
        }

        /// <summary>
        /// Creates an <see cref="IDataProtector"/> given a list of purposes.
        /// </summary>
        /// <param name="provider">The <see cref="IDataProtectionProvider"/> from which to generate the purpose chain.</param>
        /// <param name="purpose">The primary purpose used to create the <see cref="IDataProtectionProvider"/>.</param>
        /// <param name="subPurposes">An optional list of secondary purposes which contribute to the purpose chain.
        /// If this list is provided it cannot contain null elements.</param>
        /// <returns>An <see cref="IDataProtector"/> tied to the provided purpose chain.</returns>
        /// <remarks>
        /// This is a convenience method which chains together several calls to
        /// <see cref="IDataProtectionProvider.CreateProtector(string)"/>. See that method's
        /// documentation for more information.
        /// </remarks>
        public static IDataProtector CreateProtector([NotNull] this IDataProtectionProvider provider, [NotNull] string purpose, params string[] subPurposes)
        {
            // The method signature isn't simply CreateProtector(this IDataProtectionProvider, params string[] purposes)
            // because we don't want the code provider.CreateProtector() [parameterless] to inadvertently compile.
            // The actual signature for this method forces at least one purpose to be provided at the call site.

            IDataProtector protector = provider.CreateProtector(purpose);
            if (subPurposes != null && subPurposes.Length > 0)
            {
                protector = protector?.CreateProtector((IEnumerable<string>)subPurposes);
            }
            return protector ?? CryptoUtil.Fail<IDataProtector>("CreateProtector returned null.");
        }
        
        /// <summary>
        /// Cryptographically protects a piece of plaintext data.
        /// </summary>
        /// <param name="protector">The data protector to use for this operation.</param>
        /// <param name="plaintext">The plaintext data to protect.</param>
        /// <returns>The protected form of the plaintext data.</returns>
        public static string Protect([NotNull] this IDataProtector protector, [NotNull] string plaintext)
        {
            try
            {
                byte[] plaintextAsBytes = EncodingUtil.SecureUtf8Encoding.GetBytes(plaintext);
                byte[] protectedDataAsBytes = protector.Protect(plaintextAsBytes);
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
                byte[] plaintextAsBytes = protector.Unprotect(protectedDataAsBytes);
                return EncodingUtil.SecureUtf8Encoding.GetString(plaintextAsBytes);
            }
            catch (Exception ex) when (ex.RequiresHomogenization())
            {
                // Homogenize exceptions to CryptographicException
                throw Error.CryptCommon_GenericError(ex);
            }
        }
    }
}
