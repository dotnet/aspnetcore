// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNetCore.DataProtection.AuthenticatedEncryption.ConfigurationModel
{
    /// <summary>
    /// Represents a configured authenticated encryption mechanism which uses
    /// managed <see cref="System.Security.Cryptography.SymmetricAlgorithm"/> and
    /// <see cref="System.Security.Cryptography.KeyedHashAlgorithm"/> types.
    /// </summary>
    public sealed class ManagedAuthenticatedEncryptorConfiguration : IAuthenticatedEncryptorConfiguration, IInternalAuthenticatedEncryptorConfiguration
    {
        private readonly IServiceProvider _services;

        public ManagedAuthenticatedEncryptorConfiguration(ManagedAuthenticatedEncryptionOptions options)
            : this(options, services: null)
        {
        }

        public ManagedAuthenticatedEncryptorConfiguration(ManagedAuthenticatedEncryptionOptions options, IServiceProvider services)
        {
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            Options = options;
            _services = services;
        }

        public ManagedAuthenticatedEncryptionOptions Options { get; }

        public IAuthenticatedEncryptorDescriptor CreateNewDescriptor()
        {
            return this.CreateNewDescriptorCore();
        }

        IAuthenticatedEncryptorDescriptor IInternalAuthenticatedEncryptorConfiguration.CreateDescriptorFromSecret(ISecret secret)
        {
            return new ManagedAuthenticatedEncryptorDescriptor(Options, secret, _services);
        }
    }
}
