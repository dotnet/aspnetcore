// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.Framework.Internal;

namespace Microsoft.AspNet.DataProtection.AuthenticatedEncryption.ConfigurationModel
{
    /// <summary>
    /// Represents a generalized authenticated encryption mechanism.
    /// </summary>
    public unsafe sealed class AuthenticatedEncryptorConfiguration : IAuthenticatedEncryptorConfiguration, IInternalAuthenticatedEncryptorConfiguration
    {
        public AuthenticatedEncryptorConfiguration([NotNull] AuthenticatedEncryptionOptions options)
        {
            Options = options;
        }

        public AuthenticatedEncryptionOptions Options { get; }

        public IAuthenticatedEncryptorDescriptor CreateNewDescriptor()
        {
            // generate a 512-bit secret randomly
            const int KDK_SIZE_IN_BYTES = 512 / 8;
            var secret = Secret.Random(KDK_SIZE_IN_BYTES);
            return ((IInternalAuthenticatedEncryptorConfiguration)this).CreateDescriptorFromSecret(secret);
        }

        IAuthenticatedEncryptorDescriptor IInternalAuthenticatedEncryptorConfiguration.CreateDescriptorFromSecret(ISecret secret)
        {
            return new AuthenticatedEncryptorDescriptor(Options, secret);
        }
    }
}
