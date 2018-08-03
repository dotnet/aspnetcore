// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Routing.Matching
{
    /// <summary>
    /// The mutable state associated with a candidate in a <see cref="CandidateSet"/>.
    /// </summary>
    public struct CandidateState
    {
        internal CandidateState(MatcherEndpoint endpoint, int score)
        {
            Endpoint = endpoint;
            Score = score;

            IsValidCandidate = true;
            Values = null;
        }

        /// <summary>
        /// Gets the <see cref="Routing.Endpoint"/>.
        /// </summary>
        public MatcherEndpoint Endpoint { get; }

        /// <summary>
        /// Gets the score of the <see cref="Routing.Endpoint"/> within the current
        /// <see cref="CandidateSet"/>.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Candidates within a set are ordered in priority order and then assigned a
        /// sequential score value based on that ordering. Candiates with the same
        /// score are considered to have equal priority.
        /// </para>
        /// <para>
        /// The score values are used in the <see cref="EndpointSelector"/> to determine
        /// whether a set of matching candidates is an ambiguous match.
        /// </para>
        /// </remarks>
        public int Score { get; }

        /// <summary>
        /// Gets or sets a value which indicates where the <see cref="Routing.Endpoint"/> is considered
        /// a valid candiate for the current request. Set this value to <c>false</c> to exclude an
        /// <see cref="Routing.Endpoint"/> from consideration.
        /// </summary>
        public bool IsValidCandidate { get; set; }

        /// <summary>
        /// Gets or sets the <see cref="RouteValueDictionary"/> associated with the
        /// <see cref="Routing.Endpoint"/> and the current request.
        /// </summary>
        public RouteValueDictionary Values { get; set; }
    }
}
