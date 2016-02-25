// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.ComponentModel;
using System.IO;
using Microsoft.AspNetCore.DataProtection.AuthenticatedEncryption;
using Microsoft.AspNetCore.DataProtection.KeyManagement;
using Microsoft.AspNetCore.DataProtection.XmlEncryption;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Win32;

#if !DOTNET5_4 // [[ISSUE60]] Remove this #ifdef when Core CLR gets support for EncryptedXml
using System.Security.Cryptography.X509Certificates;
#endif

namespace Microsoft.AspNetCore.DataProtection.Internal
{
    /// <summary>
    /// Default implementation of <see cref="IDataProtectionBuilder"/>.
    /// </summary>
    public class DataProtectionBuilder : IDataProtectionBuilder
    {
        /// <summary>
        /// Creates a new configuration object linked to a <see cref="IServiceCollection"/>.
        /// </summary>
        public DataProtectionBuilder(IServiceCollection services)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            Services = services;
        }

        /// <inheritdoc />
        public IServiceCollection Services { get; }
    }
}
