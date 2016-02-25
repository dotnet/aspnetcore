// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNetCore.DataProtection.AuthenticatedEncryption.ConfigurationModel
{
    /// <summary>
    /// Represents a configured authenticated encryption mechanism which uses
    /// Windows CNG algorithms in GCM encryption + authentication modes.
    /// </summary>
    public sealed class CngGcmAuthenticatedEncryptorConfiguration : IAuthenticatedEncryptorConfiguration, IInternalAuthenticatedEncryptorConfiguration
    {
        private readonly IServiceProvider _services;

        public CngGcmAuthenticatedEncryptorConfiguration(CngGcmAuthenticatedEncryptionSettings settings)
            : this(settings, services: null)
        {
        }

        public CngGcmAuthenticatedEncryptorConfiguration(CngGcmAuthenticatedEncryptionSettings settings, IServiceProvider services)
        {
            if (settings == null)
            {
                throw new ArgumentNullException(nameof(settings));
            }

            Settings = settings;
            _services = services;
        }

        public CngGcmAuthenticatedEncryptionSettings Settings { get; }

        public IAuthenticatedEncryptorDescriptor CreateNewDescriptor()
        {
            return this.CreateNewDescriptorCore();
        }

        IAuthenticatedEncryptorDescriptor IInternalAuthenticatedEncryptorConfiguration.CreateDescriptorFromSecret(ISecret secret)
        {
            return new CngGcmAuthenticatedEncryptorDescriptor(Settings, secret, _services);
        }
    }
}
