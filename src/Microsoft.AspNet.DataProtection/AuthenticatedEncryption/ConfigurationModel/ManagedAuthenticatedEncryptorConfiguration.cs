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
        private readonly IServiceProvider _services;

        public ManagedAuthenticatedEncryptorConfiguration([NotNull] ManagedAuthenticatedEncryptionOptions options)
            : this(options, services: null)
        {
        }

        public ManagedAuthenticatedEncryptorConfiguration([NotNull] ManagedAuthenticatedEncryptionOptions options, IServiceProvider services)
        {
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
