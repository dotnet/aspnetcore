// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNet.Security.DataProtection.AuthenticatedEncryption
{
    /// <summary>
    /// Represents a type that can create new authenticated encryption configuration objects.
    /// </summary>
    public interface IAuthenticatedEncryptorConfigurationFactory
    {
        /// <summary>
        /// Creates a new configuration object with fresh secret key material.
        /// </summary>
        /// <returns>
        /// An IAuthenticatedEncryptorConfiguration instance.
        /// </returns>
        IAuthenticatedEncryptorConfiguration CreateNewConfiguration();
    }
}
