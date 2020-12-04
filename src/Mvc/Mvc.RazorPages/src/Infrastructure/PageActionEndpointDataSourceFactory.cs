// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.Routing;

namespace Microsoft.AspNetCore.Mvc.RazorPages.Infrastructure
{
    internal class PageActionEndpointDataSourceFactory
    {
        private readonly PageActionEndpointDataSourceIdProvider _dataSourceIdProvider;
        private readonly IActionDescriptorCollectionProvider _actions;
        private readonly ActionEndpointFactory _endpointFactory;
        private readonly PageLoader _pageLoader;

        public PageActionEndpointDataSourceFactory(
            PageActionEndpointDataSourceIdProvider dataSourceIdProvider,
            IActionDescriptorCollectionProvider actions,
            PageLoader pageLoader,
            ActionEndpointFactory endpointFactory)
        {
            _dataSourceIdProvider = dataSourceIdProvider;
            _actions = actions;
            _pageLoader = pageLoader;
            _endpointFactory = endpointFactory;
        }

        public PageActionEndpointDataSource Create(OrderedEndpointsSequenceProvider orderProvider)
        {
            return new PageActionEndpointDataSource(_dataSourceIdProvider, _actions, _endpointFactory, _pageLoader, orderProvider);
        }
    }
}
