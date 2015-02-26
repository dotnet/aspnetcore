// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNet.Cryptography;
using Microsoft.AspNet.Cryptography.Cng;
using Microsoft.AspNet.Cryptography.SafeHandles;
using Microsoft.AspNet.DataProtection.Cng;

namespace Microsoft.AspNet.DataProtection.AuthenticatedEncryption
{
    /// <summary>
    /// Options for configuring an authenticated encryption mechanism which uses
    /// Windows CNG encryption algorithms in Galois/Counter Mode.
    /// </summary>
    public sealed class CngGcmAuthenticatedEncryptorConfigurationOptions : IInternalConfigurationOptions
    {
        /// <summary>
        /// The name of the algorithm to use for symmetric encryption.
        /// This property corresponds to the 'pszAlgId' parameter of BCryptOpenAlgorithmProvider.
        /// This property is required to have a value.
        /// </summary>
        /// <remarks>
        /// The algorithm must support GCM-style encryption and must have a block size of exactly 128 bits.
        /// The default value is 'AES'.
        /// </remarks>
        public string EncryptionAlgorithm { get; set; } = Constants.BCRYPT_AES_ALGORITHM;

        /// <summary>
        /// The name of the provider which contains the implementation of the symmetric encryption algorithm.
        /// This property corresponds to the 'pszImplementation' parameter of BCryptOpenAlgorithmProvider.
        /// This property is optional.
        /// </summary>
        /// <remarks>
        /// The default value is null.
        /// </remarks>
        public string EncryptionAlgorithmProvider { get; set; } = null;

        /// <summary>
        /// The length (in bits) of the key that will be used for symmetric encryption.
        /// This property is required to have a value.
        /// </summary>
        /// <remarks>
        /// The key length must be 128 bits or greater.
        /// The default value is 256.
        /// </remarks>
        public int EncryptionAlgorithmKeySize { get; set; } = 256;

        /// <summary>
        /// Makes a duplicate of this object, which allows the original object to remain mutable.
        /// </summary>
        internal CngGcmAuthenticatedEncryptorConfigurationOptions Clone()
        {
            return new CngGcmAuthenticatedEncryptorConfigurationOptions()
            {
                EncryptionAlgorithm = this.EncryptionAlgorithm,
                EncryptionAlgorithmKeySize = this.EncryptionAlgorithmKeySize,
                EncryptionAlgorithmProvider = this.EncryptionAlgorithmProvider
            };
        }

        internal IAuthenticatedEncryptor CreateAuthenticatedEncryptor([NotNull] ISecret secret)
        {
            // Create the encryption object
            string encryptionAlgorithm = GetPropertyValueNotNullOrEmpty(EncryptionAlgorithm, nameof(EncryptionAlgorithm));
            string encryptionAlgorithmProvider = GetPropertyValueNormalizeToNull(EncryptionAlgorithmProvider);
            uint encryptionAlgorithmKeySizeInBits = GetKeySizeInBits(EncryptionAlgorithmKeySize);
            BCryptAlgorithmHandle encryptionAlgorithmHandle = GetEncryptionAlgorithmHandleAndCheckKeySize(encryptionAlgorithm, encryptionAlgorithmProvider, encryptionAlgorithmKeySizeInBits);

            // and we're good to go!
            return new GcmAuthenticatedEncryptor(
                keyDerivationKey: new Secret(secret),
                symmetricAlgorithmHandle: encryptionAlgorithmHandle,
                symmetricAlgorithmKeySizeInBytes: encryptionAlgorithmKeySizeInBits / 8);
        }

        private static BCryptAlgorithmHandle GetEncryptionAlgorithmHandleAndCheckKeySize(string encryptionAlgorithm, string encryptionAlgorithmProvider, uint keyLengthInBits)
        {
            BCryptAlgorithmHandle algorithmHandle = null;

            // Special-case cached providers
            if (encryptionAlgorithmProvider == null)
            {
                if (encryptionAlgorithm == Constants.BCRYPT_AES_ALGORITHM) { algorithmHandle = CachedAlgorithmHandles.AES_GCM; }
            }

            // Look up the provider dynamically if we couldn't fetch a cached instance
            if (algorithmHandle == null)
            {
                algorithmHandle = BCryptAlgorithmHandle.OpenAlgorithmHandle(encryptionAlgorithm, encryptionAlgorithmProvider);
                algorithmHandle.SetChainingMode(Constants.BCRYPT_CHAIN_MODE_GCM);
            }

            // make sure we're using a block cipher with an appropriate block size
            uint cipherBlockSizeInBytes = algorithmHandle.GetCipherBlockLength();
            CryptoUtil.Assert(cipherBlockSizeInBytes == 128 / 8, "cipherBlockSizeInBytes == 128 / 8");

            // make sure the provided key length is valid
            algorithmHandle.GetSupportedKeyLengths().EnsureValidKeyLength(keyLengthInBits);

            // all good!
            return algorithmHandle;
        }

        private static uint GetKeySizeInBits(int value)
        {
            CryptoUtil.Assert(value >= 0, "value >= 0");
            CryptoUtil.Assert(value % 8 == 0, "value % 8 == 0");
            return (uint)value;
        }

        private static string GetPropertyValueNormalizeToNull(string value)
        {
            return (String.IsNullOrEmpty(value)) ? null : value;
        }

        private static string GetPropertyValueNotNullOrEmpty(string value, string propertyName)
        {
            if (String.IsNullOrEmpty(value))
            {
                throw Error.Common_PropertyCannotBeNullOrEmpty(propertyName);
            }
            return value;
        }

        IAuthenticatedEncryptor IInternalConfigurationOptions.CreateAuthenticatedEncryptor(ISecret secret)
        {
            return CreateAuthenticatedEncryptor(secret);
        }
    }
}
