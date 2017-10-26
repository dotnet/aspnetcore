// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Dispatcher;
using Microsoft.Extensions.Options;

namespace Microsoft.Extensions.DependencyInjection
{
    internal class DefaultDispatcherConfigureOptions : IConfigureOptions<DispatcherOptions>
    {
        private readonly IEnumerable<DispatcherDataSource> _dataSources;
        private readonly IDefaultMatcherFactory _dispatcherFactory;
        private readonly IEnumerable<EndpointSelector> _endpointSelectors;
        private readonly IEnumerable<IHandlerFactory> _handlerFactories;

        public DefaultDispatcherConfigureOptions(
            IDefaultMatcherFactory dispatcherFactory,
            IEnumerable<DispatcherDataSource> dataSources,
            IEnumerable<EndpointSelector> endpointSelectors,
            IEnumerable<IHandlerFactory> handlerFactories)
        {
            _dispatcherFactory = dispatcherFactory;
            _dataSources = dataSources;
            _endpointSelectors = endpointSelectors;
            _handlerFactories = handlerFactories;
        }

        public void Configure(DispatcherOptions options)
        {
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            var matcher = _dispatcherFactory.CreateMatcher(new CompositeDispatcherDataSource(_dataSources), _endpointSelectors);
            
            options.Matchers.Add(new MatcherEntry()
            {
                Matcher = matcher,
                AddressProvider = matcher as IAddressCollectionProvider,
                EndpointProvider = matcher as IEndpointCollectionProvider,
                HandlerFactory = new CompositeHandlerFactory(_handlerFactories),
            });
        }
    }
}
