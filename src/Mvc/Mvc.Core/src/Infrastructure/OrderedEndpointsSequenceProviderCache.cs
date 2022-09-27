// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;
using Microsoft.AspNetCore.Routing;

namespace Microsoft.AspNetCore.Mvc.Infrastructure;

internal sealed class OrderedEndpointsSequenceProviderCache
{
    private readonly ConcurrentDictionary<IEndpointRouteBuilder, OrderedEndpointsSequenceProvider> _sequenceProviderCache = new();

    public OrderedEndpointsSequenceProvider GetOrCreateOrderedEndpointsSequenceProvider(IEndpointRouteBuilder endpoints)
    {
        return _sequenceProviderCache.GetOrAdd(endpoints, new OrderedEndpointsSequenceProvider());
    }
}
