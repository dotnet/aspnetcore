// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNet.DataProtection.AuthenticatedEncryption.ConfigurationModel;

namespace Microsoft.AspNet.DataProtection.AuthenticatedEncryption
{
    /// <summary>
    /// Implemented by our options classes to generalize creating configuration objects.
    /// </summary>
    internal interface IInternalAuthenticatedEncryptionOptions
    {
        /// <summary>
        /// Creates a <see cref="IInternalAuthenticatedEncryptorConfiguration"/> object
        /// from the given options.
        /// </summary>
        IInternalAuthenticatedEncryptorConfiguration ToConfiguration(IServiceProvider services);

        /// <summary>
        /// Performs a self-test of the algorithm specified by the options object.
        /// </summary>
        void Validate();
    }
}
