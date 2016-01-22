// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNet.DataProtection.AuthenticatedEncryption.ConfigurationModel
{
    // This type is not public because we don't want to lock ourselves into a contract stating
    // that a descriptor is simply a configuration plus a single serializable, reproducible secret.

    /// <summary>
    /// A type that knows how to create instances of an <see cref="IAuthenticatedEncryptorDescriptor"/>
    /// given specific secret key material.
    /// </summary>
    internal interface IInternalAuthenticatedEncryptorConfiguration : IAuthenticatedEncryptorConfiguration
    {
        /// <summary>
        /// Creates a new <see cref="IAuthenticatedEncryptorDescriptor"/> instance from this
        /// configuration given specific secret key material.
        /// </summary>
        IAuthenticatedEncryptorDescriptor CreateDescriptorFromSecret(ISecret secret);
    }
}
