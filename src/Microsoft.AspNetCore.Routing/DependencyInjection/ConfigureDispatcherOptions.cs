// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Options;

namespace Microsoft.Extensions.DependencyInjection
{
    internal class ConfigureDispatcherOptions : IConfigureOptions<DispatcherOptions>
    {
        private readonly IEnumerable<EndpointDataSource> _dataSources;

        public ConfigureDispatcherOptions(IEnumerable<EndpointDataSource> dataSources)
        {
            if (dataSources == null)
            {
                throw new ArgumentNullException(nameof(dataSources));
            }

            _dataSources = dataSources;
        }

        public void Configure(DispatcherOptions options)
        {
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            foreach (var dataSource in _dataSources)
            {
                options.DataSources.Add(dataSource);
            }
        }
    }
}