// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Routing.Matching
{
    internal class DfaMatcherFactory : MatcherFactory
    {
        private readonly IServiceProvider _services;

        // Using the service provider here so we can avoid coupling to the dependencies
        // of DfaMatcherBuilder.
        public DfaMatcherFactory(IServiceProvider services)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            _services = services;
        }

        public override Matcher CreateMatcher(EndpointDataSource dataSource)
        {
            if (dataSource == null)
            {
                throw new ArgumentNullException(nameof(dataSource));
            }

            return new DataSourceDependentMatcher(dataSource, () =>
            {
                return _services.GetRequiredService<DfaMatcherBuilder>();
            });
        }
    }
}
