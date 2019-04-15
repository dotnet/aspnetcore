// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections;
using System.Collections.Specialized;
using System.Runtime.CompilerServices;
using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Routing.Matching
{
    /// <summary>
    /// Represents a set of <see cref="Endpoint"/> candidates that have been matched
    /// by the routing system. Used by implementations of <see cref="EndpointSelector"/>
    /// and <see cref="IEndpointSelectorPolicy"/>.
    /// </summary>
    public sealed class CandidateSet
    {
        private const int BitVectorSize = 32;

        private readonly CandidateState[] _candidates;

        /// <summary>
        /// <para>
        /// Initializes a new instances of the <see cref="CandidateSet"/> class with the provided <paramref name="endpoints"/>,
        /// <paramref name="values"/>, and <paramref name="scores"/>.
        /// </para>
        /// <para>
        /// The constructor is provided to enable unit tests of implementations of <see cref="EndpointSelector"/>
        /// and <see cref="IEndpointSelectorPolicy"/>.
        /// </para>
        /// </summary>
        /// <param name="endpoints">The list of endpoints, sorted in descending priority order.</param>
        /// <param name="values">The list of <see cref="RouteValueDictionary"/> instances.</param>
        /// <param name="scores">The list of endpoint scores. <see cref="CandidateState.Score"/>.</param>
        public CandidateSet(Endpoint[] endpoints, RouteValueDictionary[] values, int[] scores)
        {
            if (endpoints == null)
            {
                throw new ArgumentNullException(nameof(endpoints));
            }

            if (values == null)
            {
                throw new ArgumentNullException(nameof(values));
            }

            if (scores == null)
            {
                throw new ArgumentNullException(nameof(scores));
            }

            if (endpoints.Length != values.Length || endpoints.Length != scores.Length)
            {
                throw new ArgumentException($"The provided {nameof(endpoints)}, {nameof(values)}, and {nameof(scores)} must have the same length.");
            }

            Count = endpoints.Length;

            _candidates = new CandidateState[endpoints.Length];
            for (var i = 0; i < endpoints.Length; i++)
            {
                _candidates[i] = new CandidateState(endpoints[i], values[i], scores[i]);
            }
        }

        internal CandidateSet(Candidate[] candidates)
        {
            Count = candidates.Length;

            _candidates = new CandidateState[candidates.Length];
            for (var i = 0; i < candidates.Length; i++)
            {
                _candidates[i] = new CandidateState(candidates[i].Endpoint, candidates[i].Score);
            }
        }

        /// <summary>
        /// Gets the count of candidates in the set.
        /// </summary>
        public int Count { get; }

        /// <summary>
        /// Gets the <see cref="CandidateState"/> associated with the candidate <see cref="Endpoint"/>
        /// at <paramref name="index"/>.
        /// </summary>
        /// <param name="index">The candidate index.</param>
        /// <returns>
        /// A reference to the <see cref="CandidateState"/>. The result is returned by reference.
        /// </returns>
        public ref CandidateState this[int index]
        {
            // Note that this is a ref-return because of performance.
            // We don't want to copy these fat structs if it can be avoided.

            // PERF: Force inlining
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                // Friendliness for inlining
                if ((uint)index >= Count)
                {
                    ThrowIndexArgumentOutOfRangeException();
                }

                return ref _candidates[index];
            }
        }

        /// <summary>
        /// Gets a value which indicates where the <see cref="Http.Endpoint"/> is considered
        /// a valid candiate for the current request.
        /// </summary>
        /// <param name="index">The candidate index.</param>
        /// <returns>
        /// <c>true</c> if the candidate at position <paramref name="index"/> is considered valid
        /// for the current request, otherwise <c>false</c>.
        /// </returns>
        public bool IsValidCandidate(int index)
        {
            // Friendliness for inlining
            if ((uint)index >= Count)
            {
                ThrowIndexArgumentOutOfRangeException();
            }

            return _candidates[index].Score >= 0;
        }

        /// <summary>
        /// Sets the validitity of the candidate at the provided index.
        /// </summary>
        /// <param name="index">The candidate index.</param>
        /// <param name="value">
        /// The value to set. If <c>true</c> the candidate is considered valid for the current request.
        /// </param>
        public void SetValidity(int index, bool value)
        {
            // Friendliness for inlining
            if ((uint)index >= Count)
            {
                ThrowIndexArgumentOutOfRangeException();
            }

            ref var original = ref _candidates[index];
            _candidates[index] = new CandidateState(original.Endpoint, original.Values, original.Score >= 0 ^ value ? ~original.Score : original.Score);
        }

        /// <summary>
        /// Replaces the <see cref="Endpoint"/> at the provided <paramref name="index"/> with the
        /// provided <paramref name="endpoint"/>.
        /// </summary>
        /// <param name="index">The candidate index.</param>
        /// <param name="endpoint">
        /// The <see cref="Endpoint"/> to replace the original <see cref="Endpoint"/> at
        /// the <paramref name="index"/>. If <paramref name="endpoint"/> the candidate will be marked
        /// as invalid.
        /// </param>
        /// <param name="values">
        /// The <see cref="RouteValueDictionary"/> to replace the original <see cref="RouteValueDictionary"/> at
        /// the <paramref name="index"/>.
        /// </param>
        public void ReplaceEndpoint(int index, Endpoint endpoint, RouteValueDictionary values)
        {
            // Friendliness for inlining
            if ((uint)index >= Count)
            {
                ThrowIndexArgumentOutOfRangeException();
            }

            _candidates[index] = new CandidateState(endpoint, values, _candidates[index].Score);

            if (endpoint == null)
            {
                SetValidity(index, false);
            }
        }

        private static void ThrowIndexArgumentOutOfRangeException()
        {
            throw new ArgumentOutOfRangeException("index");
        }
    }
}
