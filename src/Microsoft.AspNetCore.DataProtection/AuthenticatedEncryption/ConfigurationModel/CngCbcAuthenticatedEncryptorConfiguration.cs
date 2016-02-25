// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNetCore.DataProtection.AuthenticatedEncryption.ConfigurationModel
{
    /// <summary>
    /// Represents a configured authenticated encryption mechanism which uses
    /// Windows CNG algorithms in CBC encryption + HMAC authentication modes.
    /// </summary>
    public sealed class CngCbcAuthenticatedEncryptorConfiguration : IAuthenticatedEncryptorConfiguration, IInternalAuthenticatedEncryptorConfiguration
    {
        private readonly IServiceProvider _services;

        public CngCbcAuthenticatedEncryptorConfiguration(CngCbcAuthenticatedEncryptionSettings settings)
            : this(settings, services: null)
        {
        }

        public CngCbcAuthenticatedEncryptorConfiguration(CngCbcAuthenticatedEncryptionSettings settings, IServiceProvider services)
        {
            if (settings == null)
            {
                throw new ArgumentNullException(nameof(settings));
            }

            Settings = settings;
            _services = services;
        }

        public CngCbcAuthenticatedEncryptionSettings Settings { get; }

        public IAuthenticatedEncryptorDescriptor CreateNewDescriptor()
        {
            return this.CreateNewDescriptorCore();
        }

        IAuthenticatedEncryptorDescriptor IInternalAuthenticatedEncryptorConfiguration.CreateDescriptorFromSecret(ISecret secret)
        {
            return new CngCbcAuthenticatedEncryptorDescriptor(Settings, secret, _services);
        }
    }
}
