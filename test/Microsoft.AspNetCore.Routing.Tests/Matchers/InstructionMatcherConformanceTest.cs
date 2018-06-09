// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.AspNetCore.Routing.Matchers
{
    public class InstructionMatcherConformanceTest : MatcherConformanceTest
    {
        internal override Matcher CreateMatcher(MatcherEndpoint endpoint)
        {
            var builder = new InstructionMatcherBuilder();
            builder.AddEndpoint(endpoint);
            return builder.Build();
        }
    }
}
