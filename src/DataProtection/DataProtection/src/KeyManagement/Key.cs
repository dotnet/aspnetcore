// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.DataProtection.AuthenticatedEncryption;
using Microsoft.AspNetCore.DataProtection.AuthenticatedEncryption.ConfigurationModel;

namespace Microsoft.AspNetCore.DataProtection.KeyManagement;

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
