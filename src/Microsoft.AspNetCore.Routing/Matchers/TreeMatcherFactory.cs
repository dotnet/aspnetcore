// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Routing.EndpointConstraints;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Routing.Matchers
{
    internal class TreeMatcherFactory : MatcherFactory
    {
        private readonly IInlineConstraintResolver _constraintFactory;
        private readonly ILogger<TreeMatcher> _logger;
        private readonly EndpointSelector _endpointSelector;

        public TreeMatcherFactory(
            IInlineConstraintResolver constraintFactory,
            ILogger<TreeMatcher> logger,
            EndpointSelector endpointSelector)
        {
            if (constraintFactory == null)
            {
                throw new ArgumentNullException(nameof(constraintFactory));
            }

            if (logger == null)
            {
                throw new ArgumentNullException(nameof(logger));
            }

            if (endpointSelector == null)
            {
                throw new ArgumentNullException(nameof(endpointSelector));
            }

            _constraintFactory = constraintFactory;
            _logger = logger;
            _endpointSelector = endpointSelector;
        }

        public override Matcher CreateMatcher(EndpointDataSource dataSource)
        {
            if (dataSource == null)
            {
                throw new ArgumentNullException(nameof(dataSource));
            }

            return new TreeMatcher(_constraintFactory, _logger, dataSource, _endpointSelector);
        }
    }
}
