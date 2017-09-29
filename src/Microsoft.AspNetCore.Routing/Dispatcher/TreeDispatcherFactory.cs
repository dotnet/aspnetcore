// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Dispatcher;

namespace Microsoft.AspNetCore.Routing.Dispatcher
{
    public class TreeDispatcherFactory : IDefaultDispatcherFactory
    {
        public DispatcherEntry CreateDispatcher(DispatcherDataSource dataSource, IEnumerable<EndpointSelector> endpointSelectors)
        {
            if (dataSource == null)
            {
                throw new ArgumentNullException(nameof(dataSource));
            }

            var dispatcher = new TreeDispatcher()
            {
                DataSource = dataSource,
            };

            foreach (var endpointSelector in endpointSelectors)
            {
                dispatcher.Selectors.Add(endpointSelector);
            }

            return new DispatcherEntry()
            {
                AddressProvider = dispatcher,
                Dispatcher = dispatcher.InvokeAsync,
                EndpointProvider = dispatcher,
            };
        }
    }
}
