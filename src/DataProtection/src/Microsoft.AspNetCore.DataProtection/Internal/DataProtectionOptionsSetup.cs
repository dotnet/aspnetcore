// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.DataProtection.Internal
{
    internal class DataProtectionOptionsSetup : IConfigureOptions<DataProtectionOptions>
    {
        private readonly IServiceProvider _services;

        public DataProtectionOptionsSetup(IServiceProvider provider)
        {
            _services = provider;
        }

        public void Configure(DataProtectionOptions options)
        {
            options.ApplicationDiscriminator = _services.GetApplicationUniqueIdentifier();
        }
    }
}
