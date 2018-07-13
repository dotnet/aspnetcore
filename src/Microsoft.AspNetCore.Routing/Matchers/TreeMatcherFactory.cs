// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Routing.EndpointConstraints;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Routing.Matchers
{
    internal class TreeMatcherFactory : MatcherFactory
    {
        private readonly MatchProcessorFactory _matchProcessorFactory;
        private readonly ILogger<TreeMatcher> _logger;
        private readonly EndpointSelector _endpointSelector;

        public TreeMatcherFactory(
            MatchProcessorFactory matchProcessorFactory,
            ILogger<TreeMatcher> logger,
            EndpointSelector endpointSelector)
        {
            if (matchProcessorFactory == null)
            {
                throw new ArgumentNullException(nameof(matchProcessorFactory));
            }

            if (logger == null)
            {
                throw new ArgumentNullException(nameof(logger));
            }

            if (endpointSelector == null)
            {
                throw new ArgumentNullException(nameof(endpointSelector));
            }

            _matchProcessorFactory = matchProcessorFactory;
            _logger = logger;
            _endpointSelector = endpointSelector;
        }

        public override Matcher CreateMatcher(EndpointDataSource dataSource)
        {
            if (dataSource == null)
            {
                throw new ArgumentNullException(nameof(dataSource));
            }

            return new TreeMatcher(_matchProcessorFactory, _logger, dataSource, _endpointSelector);
        }
    }
}
