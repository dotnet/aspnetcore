// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNet.Security.DataProtection.Cng;
using Microsoft.AspNet.Security.DataProtection.SafeHandles;

namespace Microsoft.AspNet.Security.DataProtection.AuthenticatedEncryption
{
    /// <summary>
    /// Options for configuring an authenticated encryption mechanism which uses
    /// Windows CNG algorithms in CBC encryption + HMAC validation modes.
    /// </summary>
    public sealed class CngCbcAuthenticatedEncryptorConfigurationOptions : IInternalConfigurationOptions
    {
        /// <summary>
        /// The name of the algorithm to use for symmetric encryption.
        /// This property corresponds to the 'pszAlgId' parameter of BCryptOpenAlgorithmProvider.
        /// This property is required to have a value.
        /// </summary>
        /// <remarks>
        /// The algorithm must support CBC-style encryption and must have a block size of 64 bits or greater.
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
        /// The name of the algorithm to use for hashing data.
        /// This property corresponds to the 'pszAlgId' parameter of BCryptOpenAlgorithmProvider.
        /// This property is required to have a value.
        /// </summary>
        /// <remarks>
        /// The algorithm must support being opened in HMAC mode and must have a digest length
        /// of 128 bits or greater.
        /// The default value is 'SHA256'.
        /// </remarks>
        public string HashAlgorithm { get; set; } = Constants.BCRYPT_SHA256_ALGORITHM;

        /// <summary>
        /// The name of the provider which contains the implementation of the hash algorithm.
        /// This property corresponds to the 'pszImplementation' parameter of BCryptOpenAlgorithmProvider.
        /// This property is optional.
        /// </summary>
        /// <remarks>
        /// The default value is null.
        /// </remarks>
        public string HashAlgorithmProvider { get; set; } = null;

        /// <summary>
        /// Makes a duplicate of this object, which allows the original object to remain mutable.
        /// </summary>
        internal CngCbcAuthenticatedEncryptorConfigurationOptions Clone()
        {
            return new CngCbcAuthenticatedEncryptorConfigurationOptions()
            {
                EncryptionAlgorithm = this.EncryptionAlgorithm,
                EncryptionAlgorithmKeySize = this.EncryptionAlgorithmKeySize,
                EncryptionAlgorithmProvider = this.EncryptionAlgorithmProvider,
                HashAlgorithm = this.HashAlgorithm,
                HashAlgorithmProvider = this.HashAlgorithmProvider
            };
        }

        internal IAuthenticatedEncryptor CreateAuthenticatedEncryptor([NotNull] ISecret secret)
        {
            // Create the encryption object
            string encryptionAlgorithm = GetPropertyValueNotNullOrEmpty(EncryptionAlgorithm, nameof(EncryptionAlgorithm));
            string encryptionAlgorithmProvider = GetPropertyValueNormalizeToNull(EncryptionAlgorithmProvider);
            uint encryptionAlgorithmKeySizeInBits = GetKeySizeInBits(EncryptionAlgorithmKeySize);
            BCryptAlgorithmHandle encryptionAlgorithmHandle = GetEncryptionAlgorithmHandleAndCheckKeySize(encryptionAlgorithm, encryptionAlgorithmProvider, encryptionAlgorithmKeySizeInBits);

            // Create the validation object
            string hashAlgorithm = GetPropertyValueNotNullOrEmpty(HashAlgorithm, nameof(HashAlgorithm));
            string hashAlgorithmProvider = GetPropertyValueNormalizeToNull(HashAlgorithmProvider);
            BCryptAlgorithmHandle hashAlgorithmHandle = GetHashAlgorithmHandle(hashAlgorithm, hashAlgorithmProvider);

            // and we're good to go!
            return new CbcAuthenticatedEncryptor(
                keyDerivationKey: new ProtectedMemoryBlob(secret),
                symmetricAlgorithmHandle: encryptionAlgorithmHandle,
                symmetricAlgorithmKeySizeInBytes: encryptionAlgorithmKeySizeInBits / 8,
                hmacAlgorithmHandle: hashAlgorithmHandle);
        }

        private static BCryptAlgorithmHandle GetEncryptionAlgorithmHandleAndCheckKeySize(string encryptionAlgorithm, string encryptionAlgorithmProvider, uint keyLengthInBits)
        {
            BCryptAlgorithmHandle algorithmHandle = null;

            // Special-case cached providers
            if (encryptionAlgorithmProvider == null)
            {
                if (encryptionAlgorithm == Constants.BCRYPT_AES_ALGORITHM) { algorithmHandle = CachedAlgorithmHandles.AES_CBC; }
            }

            // Look up the provider dynamically if we couldn't fetch a cached instance
            if (algorithmHandle == null)
            {
                algorithmHandle = BCryptAlgorithmHandle.OpenAlgorithmHandle(encryptionAlgorithm, encryptionAlgorithmProvider);
                algorithmHandle.SetChainingMode(Constants.BCRYPT_CHAIN_MODE_CBC);
            }

            // make sure we're using a block cipher with an appropriate block size
            uint cipherBlockSizeInBytes = algorithmHandle.GetCipherBlockLength();
            CryptoUtil.Assert(cipherBlockSizeInBytes >= CbcAuthenticatedEncryptor.SYMMETRIC_ALG_MIN_BLOCK_SIZE_IN_BYTES,
                "cipherBlockSizeInBytes >= CbcAuthenticatedEncryptor.SYMMETRIC_ALG_MIN_BLOCK_SIZE_IN_BYTES");

            // make sure the provided key length is valid
            algorithmHandle.GetSupportedKeyLengths().EnsureValidKeyLength(keyLengthInBits);

            // all good!
            return algorithmHandle;
        }

        private static BCryptAlgorithmHandle GetHashAlgorithmHandle(string hashAlgorithm, string hashAlgorithmProvider)
        {
            BCryptAlgorithmHandle algorithmHandle = null;

            // Special-case cached providers
            if (hashAlgorithmProvider == null)
            {
                if (hashAlgorithm == Constants.BCRYPT_SHA1_ALGORITHM) { algorithmHandle = CachedAlgorithmHandles.HMAC_SHA1; }
                else if (hashAlgorithm == Constants.BCRYPT_SHA256_ALGORITHM) { algorithmHandle = CachedAlgorithmHandles.HMAC_SHA256; }
                else if (hashAlgorithm == Constants.BCRYPT_SHA512_ALGORITHM) { algorithmHandle = CachedAlgorithmHandles.HMAC_SHA512; }
            }

            // Look up the provider dynamically if we couldn't fetch a cached instance
            if (algorithmHandle == null)
            {
                algorithmHandle = BCryptAlgorithmHandle.OpenAlgorithmHandle(hashAlgorithm, hashAlgorithmProvider, hmac: true);
            }

            // Make sure we're using a hash algorithm. We require a minimum 128-bit digest.
            uint digestSize = algorithmHandle.GetHashDigestLength();
            CryptoUtil.Assert(digestSize >= CbcAuthenticatedEncryptor.HASH_ALG_MIN_DIGEST_LENGTH_IN_BYTES,
                "digestSize >= CbcAuthenticatedEncryptor.HASH_ALG_MIN_DIGEST_LENGTH_IN_BYTES");

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
