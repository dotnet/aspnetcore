// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.SpaProxy
{
    internal class ConfigureSpaDevelopmentServerOptions : IConfigureOptions<SpaDevelopmentServerOptions>
    {
        public void Configure(SpaDevelopmentServerOptions options)
        {
            var configuration = new ConfigurationBuilder()
                .AddJsonFile(Path.Combine(AppContext.BaseDirectory, "spa.proxy.json"))
                .Build();
            configuration.GetSection("SpaProxyServer").Bind(options);
        }
    }
}
