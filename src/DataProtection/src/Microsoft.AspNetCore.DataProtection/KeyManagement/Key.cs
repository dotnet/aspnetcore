// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.DataProtection.AuthenticatedEncryption;
using Microsoft.AspNetCore.DataProtection.AuthenticatedEncryption.ConfigurationModel;

namespace Microsoft.AspNetCore.DataProtection.KeyManagement
{
    /// <summary>
    /// The basic implementation of <see cref="IKey"/>, where the <see cref="IAuthenticatedEncryptorDescriptor"/>
    /// has already been created.
    /// </summary>
    internal sealed class Key : KeyBase
    {
        public Key(
            Guid keyId,
            DateTimeOffset creationDate,
            DateTimeOffset activationDate,
            DateTimeOffset expirationDate,
            IAuthenticatedEncryptorDescriptor descriptor,
            IEnumerable<IAuthenticatedEncryptorFactory> encryptorFactories)
            : base(keyId,
                  creationDate,
                  activationDate,
                  expirationDate,
                  new Lazy<IAuthenticatedEncryptorDescriptor>(() => descriptor),
                  encryptorFactories)
        {
        }
    }
}
