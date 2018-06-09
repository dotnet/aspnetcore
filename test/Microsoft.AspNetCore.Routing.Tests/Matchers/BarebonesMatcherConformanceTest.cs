// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.AspNetCore.Routing.Matchers
{
    public class BarebonesMatcherConformanceTest : MatcherConformanceTest
    {
        // Route values not supported
        [Fact]
        public override Task Match_SingleParameter()
        {
            return Task.CompletedTask;
        }
        
        // Route values not supported
        [Fact]
        public override Task Match_SingleParameter_TrailingSlash()
        {
            return Task.CompletedTask;
        }

        // Route values not supported
        [Theory]
        [InlineData(null, null, null, null)]
        public override Task Match_MultipleParameters(string template, string path, string[] keys, string[] values)
        {
            GC.KeepAlive(new object [] { template, path, keys, values });
            return Task.CompletedTask;
        }

        internal override Matcher CreateMatcher(MatcherEndpoint endpoint)
        {
            var builder = new BarebonesMatcherBuilder();
            builder.AddEndpoint(endpoint);
            return builder.Build();
        }
    }
}
