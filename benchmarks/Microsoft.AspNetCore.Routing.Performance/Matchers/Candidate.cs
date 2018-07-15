// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNetCore.Routing.Matchers
{
    // This is not yet fleshed out - consider this part of the
    // work-in-progress definition of CandidateSet.
    internal readonly struct Candidate
    {
        public readonly MatcherEndpoint Endpoint;

        public readonly string[] Parameters;

        public Candidate(MatcherEndpoint endpoint)
        {
            Endpoint = endpoint;
            Parameters = Array.Empty<string>();
        }

        public Candidate(MatcherEndpoint endpoint, string[] parameters)
        {
            Endpoint = endpoint;
            Parameters = parameters;
        }
    }
}
