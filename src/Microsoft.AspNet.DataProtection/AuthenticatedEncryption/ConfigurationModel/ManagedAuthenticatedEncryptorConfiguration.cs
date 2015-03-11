// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.Framework.Internal;
using System.Security.Cryptography;

namespace Microsoft.AspNet.DataProtection.AuthenticatedEncryption.ConfigurationModel
{
    /// <summary>
    /// Represents a configured authenticated encryption mechanism which uses
    /// managed <see cref="SymmetricAlgorithm"/> and <see cref="KeyedHashAlgorithm"/> types.
    /// </summary>
    public sealed class ManagedAuthenticatedEncryptorConfiguration : IAuthenticatedEncryptorConfiguration, IInternalAuthenticatedEncryptorConfiguration
    {
        public ManagedAuthenticatedEncryptorConfiguration([NotNull] ManagedAuthenticatedEncryptionOptions options)
        {
            Options = options;
        }

        public ManagedAuthenticatedEncryptionOptions Options { get; }

        public IAuthenticatedEncryptorDescriptor CreateNewDescriptor()
        {
            // generate a 512-bit secret randomly
            const int KDK_SIZE_IN_BYTES = 512 / 8;
            var secret = Secret.Random(KDK_SIZE_IN_BYTES);
            return ((IInternalAuthenticatedEncryptorConfiguration)this).CreateDescriptorFromSecret(secret);
        }

        IAuthenticatedEncryptorDescriptor IInternalAuthenticatedEncryptorConfiguration.CreateDescriptorFromSecret(ISecret secret)
        {
            return new ManagedAuthenticatedEncryptorDescriptor(Options, secret);
        }
    }
}
