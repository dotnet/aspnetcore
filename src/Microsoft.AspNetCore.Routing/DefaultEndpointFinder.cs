// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Routing
{
    internal class DefaultEndpointFinder : IEndpointFinder
    {
        private readonly CompositeEndpointDataSource _endpointDatasource;
        private readonly ILogger<DefaultEndpointFinder> _logger;

        public DefaultEndpointFinder(
            CompositeEndpointDataSource endpointDataSource,
            ILogger<DefaultEndpointFinder> logger)
        {
            _endpointDatasource = endpointDataSource;
            _logger = logger;
        }

        public IEnumerable<Endpoint> FindEndpoints(Address lookupAddress)
        {
            var allEndpoints = _endpointDatasource.Endpoints;

            if (lookupAddress == null || string.IsNullOrEmpty(lookupAddress.Name))
            {
                return allEndpoints;
            }

            var endpointsWithAddress = allEndpoints.Where(ep => ep.Address != null);
            if (!endpointsWithAddress.Any())
            {
                return allEndpoints;
            }

            foreach (var endpoint in endpointsWithAddress)
            {
                if (string.Equals(lookupAddress.Name, endpoint.Address.Name, StringComparison.OrdinalIgnoreCase))
                {
                    return new[] { endpoint };
                }
            }

            _logger.LogDebug(
                $"Could not find an endpoint having an address with name '{lookupAddress.Name}'.");

            return Enumerable.Empty<Endpoint>();
        }
    }
}
