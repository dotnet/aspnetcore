// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Reflection;
using System.Security.Cryptography;
using Microsoft.AspNet.Security.DataProtection.Managed;

namespace Microsoft.AspNet.Security.DataProtection.AuthenticatedEncryption
{
    /// <summary>
    /// Options for configuring an authenticated encryption mechanism which uses
    /// managed SymmetricAlgorithm and KeyedHashAlgorithm implementations.
    /// </summary>
    public sealed class ManagedAuthenticatedEncryptorConfigurationOptions
    {
        /// <summary>
        /// The type of the algorithm to use for symmetric encryption.
        /// This property is required to have a value.
        /// </summary>
        /// <remarks>
        /// The algorithm must support CBC-style encryption and PKCS#7 padding and must have a block size of 64 bits or greater.
        /// The default algorithm is AES.
        /// </remarks>
        public Type EncryptionAlgorithmType { get; set; } = typeof(Aes);

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
        /// A factory for the algorithm to use for validation.
        /// This property is required to have a value.
        /// </summary>
        /// <remarks>
        /// The algorithm must have a digest length of 128 bits or greater.
        /// The default algorithm is HMACSHA256.
        /// </remarks>
        public Type ValidationAlgorithmType { get; set; } = typeof(HMACSHA256);

        /// <summary>
        /// Makes a duplicate of this object, which allows the original object to remain mutable.
        /// </summary>
        internal ManagedAuthenticatedEncryptorConfigurationOptions Clone()
        {
            return new ManagedAuthenticatedEncryptorConfigurationOptions()
            {
                EncryptionAlgorithmType = this.EncryptionAlgorithmType,
                EncryptionAlgorithmKeySize = this.EncryptionAlgorithmKeySize,
                ValidationAlgorithmType = this.ValidationAlgorithmType
            };
        }

        internal IAuthenticatedEncryptor CreateAuthenticatedEncryptor([NotNull] ISecret secret)
        {
            // Create the encryption and validation object
            Func<SymmetricAlgorithm> encryptorFactory = GetEncryptionAlgorithmFactory();
            Func<KeyedHashAlgorithm> validatorFactory = GetValidationAlgorithmFactory();

            // Check key size here
            int keySizeInBits = EncryptionAlgorithmKeySize;
            CryptoUtil.Assert(keySizeInBits % 8 == 0, "keySizeInBits % 8 == 0");
            int keySizeInBytes = keySizeInBits / 8;

            // We're good to go!
            return new ManagedAuthenticatedEncryptor(
                keyDerivationKey: new ProtectedMemoryBlob(secret),
                symmetricAlgorithmFactory: encryptorFactory,
                symmetricAlgorithmKeySizeInBytes: keySizeInBytes,
                validationAlgorithmFactory: validatorFactory);
        }

        private Func<SymmetricAlgorithm> GetEncryptionAlgorithmFactory()
        {
            CryptoUtil.Assert(EncryptionAlgorithmType != null, "EncryptionAlgorithmType != null");
            CryptoUtil.Assert(typeof(SymmetricAlgorithm).IsAssignableFrom(EncryptionAlgorithmType), "typeof(SymmetricAlgorithm).IsAssignableFrom(EncryptionAlgorithmType)");

            if (EncryptionAlgorithmType == typeof(Aes))
            {
                // On Core CLR, there's no public concrete implementation of AES, so we'll special-case it here
                return Aes.Create;
            }
            else
            {
                // Otherwise the algorithm must have a default ctor
                return ((IActivator<SymmetricAlgorithm>)Activator.CreateInstance(typeof(AlgorithmActivator<>).MakeGenericType(EncryptionAlgorithmType))).Creator;
            }
        }

        private Func<KeyedHashAlgorithm> GetValidationAlgorithmFactory()
        {
            CryptoUtil.Assert(ValidationAlgorithmType != null, "ValidationAlgorithmType != null");
            CryptoUtil.Assert(typeof(KeyedHashAlgorithm).IsAssignableFrom(ValidationAlgorithmType), "typeof(KeyedHashAlgorithm).IsAssignableFrom(ValidationAlgorithmType)");

            // The algorithm must have a default ctor
            return ((IActivator<KeyedHashAlgorithm>)Activator.CreateInstance(typeof(AlgorithmActivator<>).MakeGenericType(ValidationAlgorithmType))).Creator;
        }

        private interface IActivator<out T>
        {
            Func<T> Creator { get; }
        }

        private class AlgorithmActivator<T> : IActivator<T> where T : new()
        {
            public Func<T> Creator { get; } = Activator.CreateInstance<T>;
        }
    }
}
