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

        public ManagedAuthenticatedEncryptorConfiguration(ManagedAuthenticatedEncryptionSettings settings)
            : this(settings, services: null)
        {
        }

        public ManagedAuthenticatedEncryptorConfiguration(ManagedAuthenticatedEncryptionSettings settings, IServiceProvider services)
        {
            if (settings == null)
            {
                throw new ArgumentNullException(nameof(settings));
            }

            Settings = settings;
            _services = services;
        }

        public ManagedAuthenticatedEncryptionSettings Settings { get; }

        public IAuthenticatedEncryptorDescriptor CreateNewDescriptor()
        {
            return this.CreateNewDescriptorCore();
        }

        IAuthenticatedEncryptorDescriptor IInternalAuthenticatedEncryptorConfiguration.CreateDescriptorFromSecret(ISecret secret)
        {
            return new ManagedAuthenticatedEncryptorDescriptor(Settings, secret, _services);
        }
    }
}
