// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Routing.Matchers
{
    internal class TreeMatcherFactory : MatcherFactory
    {
        private readonly IInlineConstraintResolver _constraintFactory;
        private readonly ILogger<TreeMatcher> _logger;

        public TreeMatcherFactory(IInlineConstraintResolver constraintFactory, ILogger<TreeMatcher> logger)
        {
            if (constraintFactory == null)
            {
                throw new ArgumentNullException(nameof(constraintFactory));
            }

            if (logger == null)
            {
                throw new ArgumentNullException(nameof(logger));
            }

            _constraintFactory = constraintFactory;
            _logger = logger;
        }

        public override Matcher CreateMatcher(EndpointDataSource dataSource)
        {
            if (dataSource == null)
            {
                throw new ArgumentNullException(nameof(dataSource));
            }

            return new TreeMatcher(_constraintFactory, _logger, dataSource);
        }
    }
}
