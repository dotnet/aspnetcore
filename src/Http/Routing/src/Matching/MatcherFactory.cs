// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Routing.Matching;

internal abstract class MatcherFactory
{
    public abstract Matcher CreateMatcher(EndpointDataSource dataSource);
}
