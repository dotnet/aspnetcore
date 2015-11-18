// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Security.Cryptography;
using Microsoft.AspNet.Cryptography.Cng;
using Microsoft.AspNet.DataProtection.AuthenticatedEncryption.ConfigurationModel;
using Microsoft.AspNet.DataProtection.Managed;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNet.DataProtection.AuthenticatedEncryption
{
    /// <summary>
    /// Options for configuring an authenticated encryption mechanism which uses
    /// managed SymmetricAlgorithm and KeyedHashAlgorithm implementations.
    /// </summary>
    public sealed class ManagedAuthenticatedEncryptionOptions : IInternalAuthenticatedEncryptionOptions
    {
        /// <summary>
        /// The type of the algorithm to use for symmetric encryption.
        /// The type must subclass <see cref="SymmetricAlgorithm"/>.
        /// This property is required to have a value.
        /// </summary>
        /// <remarks>
        /// The algorithm must support CBC-style encryption and PKCS#7 padding and must have a block size of 64 bits or greater.
        /// The default algorithm is AES.
        /// </remarks>
        [ApplyPolicy]
        public Type EncryptionAlgorithmType { get; set; } = typeof(Aes);

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
        /// The type of the algorithm to use for validation.
        /// Type type must subclass <see cref="KeyedHashAlgorithm"/>.
        /// This property is required to have a value.
        /// </summary>
        /// <remarks>
        /// The algorithm must have a digest length of 128 bits or greater.
        /// The default algorithm is HMACSHA256.
        /// </remarks>
        [ApplyPolicy]
        public Type ValidationAlgorithmType { get; set; } = typeof(HMACSHA256);

        /// <summary>
        /// Validates that this <see cref="ManagedAuthenticatedEncryptionOptions"/> is well-formed, i.e.,
        /// that the specified algorithms actually exist and can be instantiated properly.
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

        internal ManagedAuthenticatedEncryptor CreateAuthenticatedEncryptorInstance(ISecret secret, ILogger logger = null)
        {
            return new ManagedAuthenticatedEncryptor(
                keyDerivationKey: new Secret(secret),
                symmetricAlgorithmFactory: GetSymmetricBlockCipherAlgorithmFactory(logger),
                symmetricAlgorithmKeySizeInBytes: EncryptionAlgorithmKeySize / 8,
                validationAlgorithmFactory: GetKeyedHashAlgorithmFactory(logger));
        }

        private Func<KeyedHashAlgorithm> GetKeyedHashAlgorithmFactory(ILogger logger)
        {
            // basic argument checking
            if (ValidationAlgorithmType == null)
            {
                throw Error.Common_PropertyCannotBeNullOrEmpty(nameof(ValidationAlgorithmType));
            }

            logger?.UsingManagedKeyedHashAlgorithm(ValidationAlgorithmType.FullName);
            if (ValidationAlgorithmType == typeof(HMACSHA256))
            {
                return () => new HMACSHA256();
            }
            else if (ValidationAlgorithmType == typeof(HMACSHA512))
            {
                return () => new HMACSHA512();
            }
            else
            {
                return AlgorithmActivator.CreateFactory<KeyedHashAlgorithm>(ValidationAlgorithmType);
            }
        }

        private Func<SymmetricAlgorithm> GetSymmetricBlockCipherAlgorithmFactory(ILogger logger)
        {
            // basic argument checking
            if (EncryptionAlgorithmType == null)
            {
                throw Error.Common_PropertyCannotBeNullOrEmpty(nameof(EncryptionAlgorithmType));
            }
            typeof(SymmetricAlgorithm).AssertIsAssignableFrom(EncryptionAlgorithmType);
            if (EncryptionAlgorithmKeySize < 0)
            {
                throw Error.Common_PropertyMustBeNonNegative(nameof(EncryptionAlgorithmKeySize));
            }

            logger?.UsingManagedSymmetricAlgorithm(EncryptionAlgorithmType.FullName);

            if (EncryptionAlgorithmType == typeof(Aes))
            {
                Func<Aes> factory = null;
#if !DOTNET5_4
                if (OSVersionUtil.IsWindows())
                {
                    // If we're on desktop CLR and running on Windows, use the FIPS-compliant implementation.
                    factory = () => new AesCryptoServiceProvider();
                }
#endif
                return factory ?? Aes.Create;
            }
            else
            {
                return AlgorithmActivator.CreateFactory<SymmetricAlgorithm>(EncryptionAlgorithmType);
            }
        }

        IInternalAuthenticatedEncryptorConfiguration IInternalAuthenticatedEncryptionOptions.ToConfiguration(IServiceProvider services)
        {
            return new ManagedAuthenticatedEncryptorConfiguration(this, services);
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
