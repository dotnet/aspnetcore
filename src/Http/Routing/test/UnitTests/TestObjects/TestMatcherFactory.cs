// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Routing.Matching;

namespace Microsoft.AspNetCore.Routing.TestObjects;

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
