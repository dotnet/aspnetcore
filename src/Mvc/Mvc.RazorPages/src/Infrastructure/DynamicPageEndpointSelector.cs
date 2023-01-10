// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Routing;

namespace Microsoft.AspNetCore.Mvc.RazorPages.Infrastructure;

internal sealed class DynamicPageEndpointSelector : IDisposable
{
    private readonly EndpointDataSource _dataSource;
    private readonly DataSourceDependentCache<ActionSelectionTable<Endpoint>> _cache;

    public DynamicPageEndpointSelector(EndpointDataSource dataSource)
    {
        ArgumentNullException.ThrowIfNull(dataSource);

        _dataSource = dataSource;
        _cache = new DataSourceDependentCache<ActionSelectionTable<Endpoint>>(dataSource, Initialize);
    }

    private ActionSelectionTable<Endpoint> Table => _cache.EnsureInitialized();

    public IReadOnlyList<Endpoint> SelectEndpoints(RouteValueDictionary values)
    {
        ArgumentNullException.ThrowIfNull(values);

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
