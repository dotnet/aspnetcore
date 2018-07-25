// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Routing.Matchers
{
    public struct CandidateState
    {
        // Provided for testability
        public CandidateState(MatcherEndpoint endpoint)
            : this(endpoint, score: 0)
        {
        }

        public CandidateState(MatcherEndpoint endpoint, int score)
        {
            Endpoint = endpoint;
            Score = score;

            IsValidCandidate = true;
            Values = null;
        }

        public MatcherEndpoint Endpoint { get; }

        public int Score { get; }

        public bool IsValidCandidate { get; set; }

        public RouteValueDictionary Values { get; set; }
    }
}
