// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Routing.Matching;

namespace Microsoft.AspNetCore.Routing.TestObjects
{
    internal class TestMatcherFactory : MatcherFactory
    {
        private readonly bool _isHandled;

        public TestMatcherFactory(bool isHandled)
        {
            _isHandled = isHandled;
        }

        public override Matcher CreateMatcher(EndpointDataSource dataSource)
        {
            return new TestMatcher(_isHandled);
        }
    }
}