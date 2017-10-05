// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Dispatcher
{
    internal class DefaultDispatcherConfigureOptions : IConfigureOptions<DispatcherOptions>
    {
        private readonly IEnumerable<DispatcherDataSource> _dataSources;
        private readonly IDefaultMatcherFactory _dispatcherFactory;
        private readonly IEnumerable<EndpointSelector> _endpointSelectors;
        private readonly IEnumerable<EndpointHandlerFactoryBase> _handlerFactories;

        public DefaultDispatcherConfigureOptions(
            IDefaultMatcherFactory dispatcherFactory,
            IEnumerable<DispatcherDataSource> dataSources,
            IEnumerable<EndpointSelector> endpointSelectors,
            IEnumerable<EndpointHandlerFactoryBase> handlerFactories)
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

            options.Matchers.Add(_dispatcherFactory.CreateDispatcher(new CompositeDispatcherDataSource(_dataSources), _endpointSelectors));

            foreach (var handlerFactory in _handlerFactories)
            {
                options.HandlerFactories.Add(handlerFactory.CreateHandler);
            }
        }
    }
}
