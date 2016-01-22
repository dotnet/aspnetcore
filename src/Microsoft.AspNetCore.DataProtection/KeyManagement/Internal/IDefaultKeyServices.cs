// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNet.DataProtection.Repositories;
using Microsoft.AspNet.DataProtection.XmlEncryption;

namespace Microsoft.AspNet.DataProtection.KeyManagement.Internal
{
    /// <summary>
    /// Provides default implementations of the services required by an <see cref="IKeyManager"/>.
    /// </summary>
    public interface IDefaultKeyServices
    {
        /// <summary>
        /// Gets the default <see cref="IXmlEncryptor"/> service (could return null).
        /// </summary>
        /// <returns></returns>
        IXmlEncryptor GetKeyEncryptor();

        /// <summary>
        /// Gets the default <see cref="IXmlRepository"/> service (must not be null).
        /// </summary>
        /// <returns></returns>
        IXmlRepository GetKeyRepository();
    }
}
