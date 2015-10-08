// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNet.DataProtection.AuthenticatedEncryption.ConfigurationModel
{
    /// <summary>
    /// Represents a configured authenticated encryption mechanism which uses
    /// Windows CNG algorithms in GCM encryption + authentication modes.
    /// </summary>
    public sealed class CngGcmAuthenticatedEncryptorConfiguration : IAuthenticatedEncryptorConfiguration, IInternalAuthenticatedEncryptorConfiguration
    {
        private readonly IServiceProvider _services;

        public CngGcmAuthenticatedEncryptorConfiguration(CngGcmAuthenticatedEncryptionOptions options)
            : this(options, services: null)
        {
        }

        public CngGcmAuthenticatedEncryptorConfiguration(CngGcmAuthenticatedEncryptionOptions options, IServiceProvider services)
        {
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            Options = options;
            _services = services;
        }

        public CngGcmAuthenticatedEncryptionOptions Options { get; }

        public IAuthenticatedEncryptorDescriptor CreateNewDescriptor()
        {
            return this.CreateNewDescriptorCore();
        }

        IAuthenticatedEncryptorDescriptor IInternalAuthenticatedEncryptorConfiguration.CreateDescriptorFromSecret(ISecret secret)
        {
            return new CngGcmAuthenticatedEncryptorDescriptor(Options, secret, _services);
        }
    }
}
