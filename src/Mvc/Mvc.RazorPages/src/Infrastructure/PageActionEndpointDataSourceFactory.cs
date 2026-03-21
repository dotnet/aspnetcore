// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.Routing;

namespace Microsoft.AspNetCore.Mvc.RazorPages.Infrastructure;

internal sealed class PageActionEndpointDataSourceFactory
{
    private readonly PageActionEndpointDataSourceIdProvider _dataSourceIdProvider;
    private readonly IActionDescriptorCollectionProvider _actions;
    private readonly ActionEndpointFactory _endpointFactory;

    public PageActionEndpointDataSourceFactory(
        PageActionEndpointDataSourceIdProvider dataSourceIdProvider,
        IActionDescriptorCollectionProvider actions,
        ActionEndpointFactory endpointFactory)
    {
        _dataSourceIdProvider = dataSourceIdProvider;
        _actions = actions;
        _endpointFactory = endpointFactory;
    }

    public PageActionEndpointDataSource Create(OrderedEndpointsSequenceProvider orderProvider)
    {
        return new PageActionEndpointDataSource(
            _dataSourceIdProvider,
            _actions,
            _endpointFactory,
            orderProvider);
    }
}
