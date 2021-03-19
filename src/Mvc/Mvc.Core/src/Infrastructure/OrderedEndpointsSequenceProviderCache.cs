// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.


using System.Collections.Concurrent;
using Microsoft.AspNetCore.Routing;

namespace Microsoft.AspNetCore.Mvc.Infrastructure
{
    internal class OrderedEndpointsSequenceProviderCache
    {
        private readonly ConcurrentDictionary<IEndpointRouteBuilder, OrderedEndpointsSequenceProvider> _sequenceProviderCache = new();

        public OrderedEndpointsSequenceProvider GetOrCreateOrderedEndpointsSequenceProvider(IEndpointRouteBuilder endpoints)
        {
            return _sequenceProviderCache.GetOrAdd(endpoints, new OrderedEndpointsSequenceProvider());
        }
    }
}
