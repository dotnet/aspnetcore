// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Cryptography;
using Microsoft.AspNetCore.Cryptography.Cng;
using Microsoft.AspNetCore.Cryptography.SafeHandles;
using Microsoft.AspNetCore.DataProtection.AuthenticatedEncryption.ConfigurationModel;
using Microsoft.AspNetCore.DataProtection.Cng;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.DataProtection.AuthenticatedEncryption
{
    /// <summary>
    /// Settings for configuring an authenticated encryption mechanism which uses
    /// Windows CNG algorithms in GCM encryption + authentication modes.
    /// </summary>
    public sealed class CngGcmAuthenticatedEncryptionSettings : IInternalAuthenticatedEncryptionSettings
    {
        /// <summary>
        /// The name of the algorithm to use for symmetric encryption.
        /// This property corresponds to the 'pszAlgId' parameter of BCryptOpenAlgorithmProvider.
        /// This property is required to have a value.
        /// </summary>
        /// <remarks>
        /// The algorithm must support CBC-style encryption and must have a block size exactly
        /// 128 bits.
        /// The default value is 'AES'.
        /// </remarks>
        [ApplyPolicy]
        public string EncryptionAlgorithm { get; set; } = Constants.BCRYPT_AES_ALGORITHM;

        /// <summary>
        /// The name of the provider which contains the implementation of the symmetric encryption algorithm.
        /// This property corresponds to the 'pszImplementation' parameter of BCryptOpenAlgorithmProvider.
        /// This property is optional.
        /// </summary>
        /// <remarks>
        /// The default value is null.
        /// </remarks>
        [ApplyPolicy]
        public string EncryptionAlgorithmProvider { get; set; } = null;

        /// <summary>
        /// The length (in bits) of the key that will be used for symmetric encryption.
        /// This property is required to have a value.
        /// </summary>
        /// <remarks>
        /// The key length must be 128 bits or greater.
        /// The default value is 256.
        /// </remarks>
        [ApplyPolicy]
        public int EncryptionAlgorithmKeySize { get; set; } = 256;

        /// <summary>
        /// Validates that this <see cref="CngGcmAuthenticatedEncryptionSettings"/> is well-formed, i.e.,
        /// that the specified algorithm actually exists and can be instantiated properly.
        /// An exception will be thrown if validation fails.
        /// </summary>
        public void Validate()
        {
            // Run a sample payload through an encrypt -> decrypt operation to make sure data round-trips properly.
            using (var encryptor = CreateAuthenticatedEncryptorInstance(Secret.Random(512 / 8)))
            {
                encryptor.PerformSelfTest();
            }
        }

        /*
         * HELPER ROUTINES
         */

        internal GcmAuthenticatedEncryptor CreateAuthenticatedEncryptorInstance(ISecret secret, ILogger logger = null)
        {
            return new GcmAuthenticatedEncryptor(
                keyDerivationKey: new Secret(secret),
                symmetricAlgorithmHandle: GetSymmetricBlockCipherAlgorithmHandle(logger),
                symmetricAlgorithmKeySizeInBytes: (uint)(EncryptionAlgorithmKeySize / 8));
        }

        private BCryptAlgorithmHandle GetSymmetricBlockCipherAlgorithmHandle(ILogger logger)
        {
            // basic argument checking
            if (String.IsNullOrEmpty(EncryptionAlgorithm))
            {
                throw Error.Common_PropertyCannotBeNullOrEmpty(nameof(EncryptionAlgorithm));
            }
            if (EncryptionAlgorithmKeySize < 0)
            {
                throw Error.Common_PropertyMustBeNonNegative(nameof(EncryptionAlgorithmKeySize));
            }

            BCryptAlgorithmHandle algorithmHandle = null;

            logger?.OpeningCNGAlgorithmFromProviderWithChainingModeGCM(EncryptionAlgorithm, EncryptionAlgorithmProvider);
            // Special-case cached providers
            if (EncryptionAlgorithmProvider == null)
            {
                if (EncryptionAlgorithm == Constants.BCRYPT_AES_ALGORITHM) { algorithmHandle = CachedAlgorithmHandles.AES_GCM; }
            }

            // Look up the provider dynamically if we couldn't fetch a cached instance
            if (algorithmHandle == null)
            {
                algorithmHandle = BCryptAlgorithmHandle.OpenAlgorithmHandle(EncryptionAlgorithm, EncryptionAlgorithmProvider);
                algorithmHandle.SetChainingMode(Constants.BCRYPT_CHAIN_MODE_GCM);
            }

            // make sure we're using a block cipher with an appropriate key size & block size
            CryptoUtil.Assert(algorithmHandle.GetCipherBlockLength() == 128 / 8, "GCM requires a block cipher algorithm with a 128-bit block size.");
            AlgorithmAssert.IsAllowableSymmetricAlgorithmKeySize(checked((uint)EncryptionAlgorithmKeySize));

            // make sure the provided key length is valid
            algorithmHandle.GetSupportedKeyLengths().EnsureValidKeyLength((uint)EncryptionAlgorithmKeySize);

            // all good!
            return algorithmHandle;
        }

        IInternalAuthenticatedEncryptorConfiguration IInternalAuthenticatedEncryptionSettings.ToConfiguration(IServiceProvider services)
        {
            return new CngGcmAuthenticatedEncryptorConfiguration(this, services);
        }
    }
}
