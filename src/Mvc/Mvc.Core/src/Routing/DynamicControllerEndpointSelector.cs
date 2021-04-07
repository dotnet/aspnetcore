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
        private readonly DataSourceDependentCache<ActionSelectionTable<Endpoint>> _cache;

        public DynamicControllerEndpointSelector(EndpointDataSource dataSource)
        {
            if (dataSource == null)
            {
                throw new ArgumentNullException(nameof(dataSource));
            }

            _cache = new DataSourceDependentCache<ActionSelectionTable<Endpoint>>(dataSource, Initialize);
        }

        private ActionSelectionTable<Endpoint> Table => _cache.EnsureInitialized();

        public IReadOnlyList<Endpoint> SelectEndpoints(RouteValueDictionary values)
        {
            if (values == null)
            {
                throw new ArgumentNullException(nameof(values));
            }

            var table = Table;
            var matches = table.Select(values);
            return matches;
        }

        private static ActionSelectionTable<Endpoint> Initialize(IReadOnlyList<Endpoint> endpoints)
        {
            return ActionSelectionTable<Endpoint>.Create(endpoints);
        }

        public void Dispose()
        {
            _cache.Dispose();
        }
    }
}
