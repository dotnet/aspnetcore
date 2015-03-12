// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.Framework.Internal;

namespace Microsoft.AspNet.DataProtection.AuthenticatedEncryption.ConfigurationModel
{
    /// <summary>
    /// Represents a generalized authenticated encryption mechanism.
    /// </summary>
    public sealed class AuthenticatedEncryptorConfiguration : IAuthenticatedEncryptorConfiguration, IInternalAuthenticatedEncryptorConfiguration
    {
        private readonly IServiceProvider _services;

        public AuthenticatedEncryptorConfiguration([NotNull] AuthenticatedEncryptionOptions options)
            : this(options, services: null)
        {
        }

        public AuthenticatedEncryptorConfiguration([NotNull] AuthenticatedEncryptionOptions options, IServiceProvider services)
        {
            Options = options;
            _services = services;
        }

        public AuthenticatedEncryptionOptions Options { get; }

        public IAuthenticatedEncryptorDescriptor CreateNewDescriptor()
        {
            return this.CreateNewDescriptorCore();
        }
        
        IAuthenticatedEncryptorDescriptor IInternalAuthenticatedEncryptorConfiguration.CreateDescriptorFromSecret(ISecret secret)
        {
            return new AuthenticatedEncryptorDescriptor(Options, secret, _services);
        }
    }
}
