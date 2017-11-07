// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;

namespace Microsoft.AspNetCore.Dispatcher
{
    public class TreeMatcherFactory : IDefaultMatcherFactory
    {
        public IMatcher CreateMatcher(DispatcherDataSource dataSource, IEnumerable<EndpointSelector> endpointSelectors)
        {
            if (dataSource == null)
            {
                throw new ArgumentNullException(nameof(dataSource));
            }

            var matcher = new TreeMatcher()
            {
                DataSource = dataSource,
            };

            foreach (var endpointSelector in endpointSelectors)
            {
                matcher.Selectors.Add(endpointSelector);
            }

            return matcher;
        }
    }
}
