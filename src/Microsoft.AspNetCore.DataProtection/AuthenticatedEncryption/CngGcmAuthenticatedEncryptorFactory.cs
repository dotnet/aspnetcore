// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Cryptography;
using Microsoft.AspNetCore.Cryptography.Cng;
using Microsoft.AspNetCore.Cryptography.SafeHandles;
using Microsoft.AspNetCore.DataProtection.AuthenticatedEncryption.ConfigurationModel;
using Microsoft.AspNetCore.DataProtection.Cng;
using Microsoft.AspNetCore.DataProtection.KeyManagement;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.DataProtection.AuthenticatedEncryption
{
    /// <summary>
    /// An <see cref="IAuthenticatedEncryptorFactory"/> for <see cref="GcmAuthenticatedEncryptor"/>.
    /// </summary>
    public sealed class CngGcmAuthenticatedEncryptorFactory : IAuthenticatedEncryptorFactory
    {
        private readonly ILogger _logger;

        public CngGcmAuthenticatedEncryptorFactory(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<CngGcmAuthenticatedEncryptorFactory>();
        }

        public IAuthenticatedEncryptor CreateEncryptorInstance(IKey key)
        {
            var descriptor = key.Descriptor as CngGcmAuthenticatedEncryptorDescriptor;
            if (descriptor == null)
            {
                return null;
            }

            return CreateAuthenticatedEncryptorInstance(descriptor.MasterKey, descriptor.Configuration);
        }

        internal GcmAuthenticatedEncryptor CreateAuthenticatedEncryptorInstance(
            ISecret secret,
            CngGcmAuthenticatedEncryptorConfiguration configuration)
        {
            if (configuration == null)
            {
                return null;
            }

            return new GcmAuthenticatedEncryptor(
                keyDerivationKey: new Secret(secret),
                symmetricAlgorithmHandle: GetSymmetricBlockCipherAlgorithmHandle(configuration),
                symmetricAlgorithmKeySizeInBytes: (uint)(configuration.EncryptionAlgorithmKeySize / 8));
        }

        private BCryptAlgorithmHandle GetSymmetricBlockCipherAlgorithmHandle(CngGcmAuthenticatedEncryptorConfiguration configuration)
        {
            // basic argument checking
            if (String.IsNullOrEmpty(configuration.EncryptionAlgorithm))
            {
                throw Error.Common_PropertyCannotBeNullOrEmpty(nameof(EncryptionAlgorithm));
            }
            if (configuration.EncryptionAlgorithmKeySize < 0)
            {
                throw Error.Common_PropertyMustBeNonNegative(nameof(configuration.EncryptionAlgorithmKeySize));
            }

            BCryptAlgorithmHandle algorithmHandle = null;

            _logger?.OpeningCNGAlgorithmFromProviderWithChainingModeGCM(configuration.EncryptionAlgorithm, configuration.EncryptionAlgorithmProvider);
            // Special-case cached providers
            if (configuration.EncryptionAlgorithmProvider == null)
            {
                if (configuration.EncryptionAlgorithm == Constants.BCRYPT_AES_ALGORITHM) { algorithmHandle = CachedAlgorithmHandles.AES_GCM; }
            }

            // Look up the provider dynamically if we couldn't fetch a cached instance
            if (algorithmHandle == null)
            {
                algorithmHandle = BCryptAlgorithmHandle.OpenAlgorithmHandle(configuration.EncryptionAlgorithm, configuration.EncryptionAlgorithmProvider);
                algorithmHandle.SetChainingMode(Constants.BCRYPT_CHAIN_MODE_GCM);
            }

            // make sure we're using a block cipher with an appropriate key size & block size
            CryptoUtil.Assert(algorithmHandle.GetCipherBlockLength() == 128 / 8, "GCM requires a block cipher algorithm with a 128-bit block size.");
            AlgorithmAssert.IsAllowableSymmetricAlgorithmKeySize(checked((uint)configuration.EncryptionAlgorithmKeySize));

            // make sure the provided key length is valid
            algorithmHandle.GetSupportedKeyLengths().EnsureValidKeyLength((uint)configuration.EncryptionAlgorithmKeySize);

            // all good!
            return algorithmHandle;
        }
    }
}
