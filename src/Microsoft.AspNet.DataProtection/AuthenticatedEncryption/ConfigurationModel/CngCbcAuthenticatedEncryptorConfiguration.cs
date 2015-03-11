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
    public unsafe sealed class CngCbcAuthenticatedEncryptorConfiguration : IAuthenticatedEncryptorConfiguration, IInternalAuthenticatedEncryptorConfiguration
    {
        public CngCbcAuthenticatedEncryptorConfiguration([NotNull] CngCbcAuthenticatedEncryptionOptions options)
        {
            Options = options;
        }

        public CngCbcAuthenticatedEncryptionOptions Options { get; }

        public IAuthenticatedEncryptorDescriptor CreateNewDescriptor()
        {
            // generate a 512-bit secret randomly
            const int KDK_SIZE_IN_BYTES = 512 / 8;
            var secret = Secret.Random(KDK_SIZE_IN_BYTES);
            return ((IInternalAuthenticatedEncryptorConfiguration)this).CreateDescriptorFromSecret(secret);
        }

        IAuthenticatedEncryptorDescriptor IInternalAuthenticatedEncryptorConfiguration.CreateDescriptorFromSecret(ISecret secret)
        {
            return new CngCbcAuthenticatedEncryptorDescriptor(Options, secret);
        }
    }
}
