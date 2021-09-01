// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Options;

namespace Microsoft.Extensions.DependencyInjection
{
    internal class ConfigureRouteOptions : IConfigureOptions<RouteOptions>
    {
        private readonly ICollection<EndpointDataSource> _dataSources;

        public ConfigureRouteOptions(ICollection<EndpointDataSource> dataSources)
        {
            if (dataSources == null)
            {
                throw new ArgumentNullException(nameof(dataSources));
            }

            _dataSources = dataSources;
        }

        public void Configure(RouteOptions options)
        {
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            options.EndpointDataSources = _dataSources;
        }
    }
}