// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Routing;

namespace Microsoft.AspNetCore.Mvc.Routing
{
    internal class DynamicControllerEndpointSelector : IDisposable
    {
        private readonly ControllerActionEndpointDataSource _dataSource;
        private readonly DataSourceDependentCache<ActionSelectionTable<RouteEndpoint>> _cache;

        public DynamicControllerEndpointSelector(ControllerActionEndpointDataSource dataSource)
        {
            if (dataSource == null)
            {
                throw new ArgumentNullException(nameof(dataSource));
            }

            _dataSource = dataSource;
            _cache = new DataSourceDependentCache<ActionSelectionTable<RouteEndpoint>>(dataSource, Initialize);
        }

        private ActionSelectionTable<RouteEndpoint> Table => _cache.EnsureInitialized();

        public IReadOnlyList<RouteEndpoint> SelectEndpoints(RouteValueDictionary values)
        {
            if (values == null)
            {
                throw new ArgumentNullException(nameof(values));
            }

            var table = Table;
            var matches = table.Select(values);
            return matches;
        }

        private static ActionSelectionTable<RouteEndpoint> Initialize(IReadOnlyList<Endpoint> endpoints)
        {
            return ActionSelectionTable<RouteEndpoint>.Create(endpoints);
        }

        public void Dispose()
        {
            _cache.Dispose();
        }
    }
}
