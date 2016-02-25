// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.DataProtection.AuthenticatedEncryption.ConfigurationModel;

namespace Microsoft.AspNetCore.DataProtection.AuthenticatedEncryption
{
    /// <summary>
    /// Implemented by our settings classes to generalize creating configuration objects.
    /// </summary>
    internal interface IInternalAuthenticatedEncryptionSettings
    {
        /// <summary>
        /// Creates a <see cref="IInternalAuthenticatedEncryptorConfiguration"/> object
        /// from the given settings.
        /// </summary>
        IInternalAuthenticatedEncryptorConfiguration ToConfiguration(IServiceProvider services);

        /// <summary>
        /// Performs a self-test of the algorithm specified by the settings object.
        /// </summary>
        void Validate();
    }
}
