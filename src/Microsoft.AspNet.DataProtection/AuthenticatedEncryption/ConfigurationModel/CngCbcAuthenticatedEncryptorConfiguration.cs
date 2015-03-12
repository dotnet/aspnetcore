// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.Framework.Internal;

namespace Microsoft.AspNet.DataProtection.AuthenticatedEncryption.ConfigurationModel
{
    /// <summary>
    /// Represents a configured authenticated encryption mechanism which uses
    /// Windows CNG algorithms in CBC encryption + HMAC authentication modes.
    /// </summary>
    public sealed class CngCbcAuthenticatedEncryptorConfiguration : IAuthenticatedEncryptorConfiguration, IInternalAuthenticatedEncryptorConfiguration
    {
        private readonly IServiceProvider _services;

        public CngCbcAuthenticatedEncryptorConfiguration([NotNull] CngCbcAuthenticatedEncryptionOptions options)
            : this(options, services: null)
        {
        }

        public CngCbcAuthenticatedEncryptorConfiguration([NotNull] CngCbcAuthenticatedEncryptionOptions options, IServiceProvider services)
        {
            Options = options;
            _services = services;
        }

        public CngCbcAuthenticatedEncryptionOptions Options { get; }

        public IAuthenticatedEncryptorDescriptor CreateNewDescriptor()
        {
            return this.CreateNewDescriptorCore();
        }

        IAuthenticatedEncryptorDescriptor IInternalAuthenticatedEncryptorConfiguration.CreateDescriptorFromSecret(ISecret secret)
        {
            return new CngCbcAuthenticatedEncryptorDescriptor(Options, secret, _services);
        }
    }
}
