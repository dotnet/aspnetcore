// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.DataProtection.KeyManagement;
using Microsoft.AspNetCore.DataProtection.Repositories;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System;

namespace Microsoft.AspNetCore.DataProtection.EntityFrameworkCore
{
    internal class ConfigureKeyManagementOptions : IConfigureOptions<KeyManagementOptions>
    {
        private readonly IServiceProvider _serviceProvider;

        public ConfigureKeyManagementOptions(IServiceProvider serviceProvider)
            => _serviceProvider = serviceProvider;

        public void Configure(KeyManagementOptions options)
            => options.XmlRepository = _serviceProvider.CreateScope().ServiceProvider.GetRequiredService<IXmlRepository>();
    }
}