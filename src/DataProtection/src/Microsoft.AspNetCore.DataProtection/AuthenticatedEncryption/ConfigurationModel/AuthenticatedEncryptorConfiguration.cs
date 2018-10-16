// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Cryptography;
using Microsoft.Extensions.Logging.Abstractions;

namespace Microsoft.AspNetCore.DataProtection.AuthenticatedEncryption.ConfigurationModel
{
    /// <summary>
    /// Represents a generalized authenticated encryption mechanism.
    /// </summary>
    public sealed class AuthenticatedEncryptorConfiguration : AlgorithmConfiguration, IInternalAlgorithmConfiguration
    {
        /// <summary>
        /// The algorithm to use for symmetric encryption (confidentiality).
        /// </summary>
        /// <remarks>
        /// The default value is <see cref="EncryptionAlgorithm.AES_256_CBC"/>.
        /// </remarks>
        public EncryptionAlgorithm EncryptionAlgorithm { get; set; } = EncryptionAlgorithm.AES_256_CBC;

        /// <summary>
        /// The algorithm to use for message authentication (tamper-proofing).
        /// </summary>
        /// <remarks>
        /// The default value is <see cref="ValidationAlgorithm.HMACSHA256"/>.
        /// This property is ignored if <see cref="EncryptionAlgorithm"/> specifies a 'GCM' algorithm.
        /// </remarks>
        public ValidationAlgorithm ValidationAlgorithm { get; set; } = ValidationAlgorithm.HMACSHA256;

        public override IAuthenticatedEncryptorDescriptor CreateNewDescriptor()
        {
            var internalConfiguration = (IInternalAlgorithmConfiguration)this;
            return internalConfiguration.CreateDescriptorFromSecret(Secret.Random(KDK_SIZE_IN_BYTES));
        }

        IAuthenticatedEncryptorDescriptor IInternalAlgorithmConfiguration.CreateDescriptorFromSecret(ISecret secret)
        {
            return new AuthenticatedEncryptorDescriptor(this, secret);
        }

        void IInternalAlgorithmConfiguration.Validate()
        {
            var factory = new AuthenticatedEncryptorFactory(NullLoggerFactory.Instance);
            // Run a sample payload through an encrypt -> decrypt operation to make sure data round-trips properly.
            var encryptor = factory.CreateAuthenticatedEncryptorInstance(Secret.Random(512 / 8), this);
            try
            {
                encryptor.PerformSelfTest();
            }
            finally
            {
                (encryptor as IDisposable)?.Dispose();
            }
        }
    }
}
