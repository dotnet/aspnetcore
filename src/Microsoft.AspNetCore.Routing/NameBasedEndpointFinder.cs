// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Routing.Matchers;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Routing
{
    internal class NameBasedEndpointFinder : IEndpointFinder<string>
    {
        private readonly CompositeEndpointDataSource _endpointDatasource;
        private readonly ILogger<NameBasedEndpointFinder> _logger;

        public NameBasedEndpointFinder(
            CompositeEndpointDataSource endpointDataSource,
            ILogger<NameBasedEndpointFinder> logger)
        {
            _endpointDatasource = endpointDataSource;
            _logger = logger;
        }

        public IEnumerable<Endpoint> FindEndpoints(string endpointName)
        {
            if (string.IsNullOrEmpty(endpointName))
            {
                return Array.Empty<Endpoint>();
            }

            var endpoints = _endpointDatasource.Endpoints.OfType<MatcherEndpoint>();

            foreach (var endpoint in endpoints)
            {
                var nameMetadata = endpoint.Metadata.GetMetadata<INameMetadata>();
                if (nameMetadata != null &&
                    string.Equals(endpointName, nameMetadata.Name, StringComparison.OrdinalIgnoreCase))
                {
                    return new[] { endpoint };
                }
            }
            return Array.Empty<Endpoint>();
        }
    }
}
