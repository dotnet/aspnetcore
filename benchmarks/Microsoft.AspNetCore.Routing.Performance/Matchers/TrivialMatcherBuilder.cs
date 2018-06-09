// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;

namespace Microsoft.AspNetCore.Routing.Matchers
{
    internal class TrivialMatcherBuilder : MatcherBuilder
    {
        private readonly List<MatcherEndpoint> _endpoints = new List<MatcherEndpoint>();

        public override void AddEndpoint(MatcherEndpoint endpoint)
        {
            _endpoints.Add(endpoint);
        }

        public override Matcher Build()
        {
            return new TrivialMatcher(_endpoints.Last());
        }
    }
}
