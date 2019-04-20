// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading.Tasks;
using Xunit;

namespace Microsoft.AspNetCore.Routing.Matching
{
    public class TreeRouterMatcherConformanceTest : FullFeaturedMatcherConformanceTest
    {
        // TreeRouter doesn't support non-inline default values.
        [Fact]
        public override Task Match_NonInlineDefaultValues()
        {
            return Task.CompletedTask;
        }

        // TreeRouter doesn't support non-inline default values.
        [Fact]
        public override Task Match_ExtraDefaultValues()
        {
            return Task.CompletedTask;
        }

        internal override Matcher CreateMatcher(params RouteEndpoint[] endpoints)
        {
            var builder = new TreeRouterMatcherBuilder();
            for (var i = 0; i < endpoints.Length; i++)
            {
                builder.AddEndpoint(endpoints[i]);
            }
            return builder.Build();
        }
    }
}
