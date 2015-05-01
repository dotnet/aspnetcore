// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNet.DataProtection.AuthenticatedEncryption.ConfigurationModel
{
    /// <summary>
    /// The basic configuration that serves as a factory for types related to authenticated encryption.
    /// </summary>
    public interface IAuthenticatedEncryptorConfiguration
    {
        /// <summary>
        /// Creates a new <see cref="IAuthenticatedEncryptorDescriptor"/> instance based on this
        /// configuration. The newly-created instance contains unique key material and is distinct
        /// from all other descriptors created by the <see cref="CreateNewDescriptor"/> method.
        /// </summary>
        /// <returns>A unique <see cref="IAuthenticatedEncryptorDescriptor"/>.</returns>
        IAuthenticatedEncryptorDescriptor CreateNewDescriptor();
    }
}
