// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Cryptography.Cng;
using Microsoft.AspNetCore.DataProtection.AuthenticatedEncryption.ConfigurationModel;
using Microsoft.AspNetCore.DataProtection.KeyManagement;
using Microsoft.AspNetCore.DataProtection.Managed;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.DataProtection.AuthenticatedEncryption
{
    /// <summary>
    /// An <see cref="IAuthenticatedEncryptorFactory"/> for <see cref="ManagedAuthenticatedEncryptor"/>.
    /// </summary>
    public sealed class ManagedAuthenticatedEncryptorFactory : IAuthenticatedEncryptorFactory
    {
        private readonly ILogger _logger;

        public ManagedAuthenticatedEncryptorFactory(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<ManagedAuthenticatedEncryptorFactory>();
        }

        public IAuthenticatedEncryptor CreateEncryptorInstance(IKey key)
        {
            var descriptor = key.Descriptor as ManagedAuthenticatedEncryptorDescriptor;
            if (descriptor == null)
            {
                return null;
            }

            return CreateAuthenticatedEncryptorInstance(descriptor.MasterKey, descriptor.Configuration);
        }

        internal ManagedAuthenticatedEncryptor CreateAuthenticatedEncryptorInstance(
            ISecret secret,
            ManagedAuthenticatedEncryptorConfiguration configuration)
        {
            if (configuration == null)
            {
                return null;
            }

            return new ManagedAuthenticatedEncryptor(
                keyDerivationKey: new Secret(secret),
                symmetricAlgorithmFactory: GetSymmetricBlockCipherAlgorithmFactory(configuration),
                symmetricAlgorithmKeySizeInBytes: configuration.EncryptionAlgorithmKeySize / 8,
                validationAlgorithmFactory: GetKeyedHashAlgorithmFactory(configuration));
        }

        private Func<KeyedHashAlgorithm> GetKeyedHashAlgorithmFactory(ManagedAuthenticatedEncryptorConfiguration configuration)
        {
            // basic argument checking
            if (configuration.ValidationAlgorithmType == null)
            {
                throw Error.Common_PropertyCannotBeNullOrEmpty(nameof(configuration.ValidationAlgorithmType));
            }

            _logger.UsingManagedKeyedHashAlgorithm(configuration.ValidationAlgorithmType.FullName);
            if (configuration.ValidationAlgorithmType == typeof(HMACSHA256))
            {
                return () => new HMACSHA256();
            }
            else if (configuration.ValidationAlgorithmType == typeof(HMACSHA512))
            {
                return () => new HMACSHA512();
            }
            else
            {
                return AlgorithmActivator.CreateFactory<KeyedHashAlgorithm>(configuration.ValidationAlgorithmType);
            }
        }

        private Func<SymmetricAlgorithm> GetSymmetricBlockCipherAlgorithmFactory(ManagedAuthenticatedEncryptorConfiguration configuration)
        {
            // basic argument checking
            if (configuration.EncryptionAlgorithmType == null)
            {
                throw Error.Common_PropertyCannotBeNullOrEmpty(nameof(configuration.EncryptionAlgorithmType));
            }
            typeof(SymmetricAlgorithm).AssertIsAssignableFrom(configuration.EncryptionAlgorithmType);
            if (configuration.EncryptionAlgorithmKeySize < 0)
            {
                throw Error.Common_PropertyMustBeNonNegative(nameof(configuration.EncryptionAlgorithmKeySize));
            }

            _logger.UsingManagedSymmetricAlgorithm(configuration.EncryptionAlgorithmType.FullName);

            if (configuration.EncryptionAlgorithmType == typeof(Aes))
            {
                Func<Aes> factory = null;
                if (OSVersionUtil.IsWindows())
                {
                    // If we're on desktop CLR and running on Windows, use the FIPS-compliant implementation.
                    factory = () => new AesCryptoServiceProvider();
                }

                return factory ?? Aes.Create;
            }
            else
            {
                return AlgorithmActivator.CreateFactory<SymmetricAlgorithm>(configuration.EncryptionAlgorithmType);
            }
        }

        /// <summary>
        /// Contains helper methods for generating cryptographic algorithm factories.
        /// </summary>
        private static class AlgorithmActivator
        {
            /// <summary>
            /// Creates a factory that wraps a call to <see cref="Activator.CreateInstance{T}"/>.
            /// </summary>
            public static Func<T> CreateFactory<T>(Type implementation)
            {
                return ((IActivator<T>)Activator.CreateInstance(typeof(AlgorithmActivatorCore<>).MakeGenericType(implementation))).Creator;
            }

            private interface IActivator<out T>
            {
                Func<T> Creator { get; }
            }

            private class AlgorithmActivatorCore<T> : IActivator<T> where T : new()
            {
                public Func<T> Creator { get; } = Activator.CreateInstance<T>;
            }
        }
    }
}
