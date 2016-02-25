// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNetCore.DataProtection.AuthenticatedEncryption.ConfigurationModel
{
    /// <summary>
    /// Represents a generalized authenticated encryption mechanism.
    /// </summary>
    public sealed class AuthenticatedEncryptorConfiguration : IAuthenticatedEncryptorConfiguration, IInternalAuthenticatedEncryptorConfiguration
    {
        private readonly IServiceProvider _services;

        public AuthenticatedEncryptorConfiguration(AuthenticatedEncryptionSettings settings)
            : this(settings, services: null)
        {
        }

        public AuthenticatedEncryptorConfiguration(AuthenticatedEncryptionSettings settings, IServiceProvider services)
        {
            if (settings == null)
            {
                throw new ArgumentNullException(nameof(settings));
            }

            Settings = settings;
            _services = services;
        }

        public AuthenticatedEncryptionSettings Settings { get; }

        public IAuthenticatedEncryptorDescriptor CreateNewDescriptor()
        {
            return this.CreateNewDescriptorCore();
        }

        IAuthenticatedEncryptorDescriptor IInternalAuthenticatedEncryptorConfiguration.CreateDescriptorFromSecret(ISecret secret)
        {
            return new AuthenticatedEncryptorDescriptor(Settings, secret, _services);
        }
    }
}
