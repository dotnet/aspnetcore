// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Routing.Matching
{
    public class RouteMatcherConformanceTest : FullFeaturedMatcherConformanceTest
    {
        internal override Matcher CreateMatcher(params RouteEndpoint[] endpoints)
        {
            var builder = new RouteMatcherBuilder();
            for (var i = 0; i < endpoints.Length; i++)
            {
                builder.AddEndpoint(endpoints[i]); 
            }
            return builder.Build();
        }
    }
}
