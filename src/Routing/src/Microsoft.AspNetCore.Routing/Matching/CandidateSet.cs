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

        // Cannot be readonly because we need to modify it in place.
        private BitVector32 _validity;
        private readonly BitArray _largeCapactityValidity;

        // We inline storage for 4 candidates here to avoid allocations in common
        // cases. There's no real reason why 4 is important, it just seemed like 
        // a plausible number.
        private CandidateState _state0;
        private CandidateState _state1;
        private CandidateState _state2;
        private CandidateState _state3;

        private CandidateState[] _additionalCandidates;

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

            switch (endpoints.Length)
            {
                case 0:
                    return;

                case 1:
                    _state0 = new CandidateState(endpoints[0], values[0], scores[0]);
                    break;

                case 2:
                    _state0 = new CandidateState(endpoints[0], values[0], scores[0]);
                    _state1 = new CandidateState(endpoints[1], values[1], scores[1]);
                    break;

                case 3:
                    _state0 = new CandidateState(endpoints[0], values[0], scores[0]);
                    _state1 = new CandidateState(endpoints[1], values[1], scores[1]);
                    _state2 = new CandidateState(endpoints[2], values[2], scores[2]);
                    break;

                case 4:
                    _state0 = new CandidateState(endpoints[0], values[0], scores[0]);
                    _state1 = new CandidateState(endpoints[1], values[1], scores[1]);
                    _state2 = new CandidateState(endpoints[2], values[2], scores[2]);
                    _state3 = new CandidateState(endpoints[3], values[3], scores[3]);
                    break;

                default:
                    _state0 = new CandidateState(endpoints[0], values[0], scores[0]);
                    _state1 = new CandidateState(endpoints[1], values[1], scores[1]);
                    _state2 = new CandidateState(endpoints[2], values[2], scores[2]);
                    _state3 = new CandidateState(endpoints[3], values[3], scores[3]);

                    _additionalCandidates = new CandidateState[endpoints.Length - 4];
                    for (var i = 4; i < endpoints.Length; i++)
                    {
                        _additionalCandidates[i - 4] = new CandidateState(endpoints[i], values[i], scores[i]);
                    }
                    break;
            }

            // Initialize validity to valid by default.
            if (Count < BitVectorSize)
            {
                // Sets the bit for each candidate that exists (bits > Count will be 0).
                _validity = new BitVector32(unchecked((int)~(0xFFFFFFFFu << Count)));
            }
            else
            {
                _largeCapactityValidity = new BitArray(Count, defaultValue: true);
            }
        }

        internal CandidateSet(Candidate[] candidates)
        {
            Count = candidates.Length;

            switch (candidates.Length)
            {
                case 0:
                    return;

                case 1:
                    _state0 = new CandidateState(candidates[0].Endpoint, candidates[0].Score);
                    break;

                case 2:
                    _state0 = new CandidateState(candidates[0].Endpoint, candidates[0].Score);
                    _state1 = new CandidateState(candidates[1].Endpoint, candidates[1].Score);
                    break;

                case 3:
                    _state0 = new CandidateState(candidates[0].Endpoint, candidates[0].Score);
                    _state1 = new CandidateState(candidates[1].Endpoint, candidates[1].Score);
                    _state2 = new CandidateState(candidates[2].Endpoint, candidates[2].Score);
                    break;

                case 4:
                    _state0 = new CandidateState(candidates[0].Endpoint, candidates[0].Score);
                    _state1 = new CandidateState(candidates[1].Endpoint, candidates[1].Score);
                    _state2 = new CandidateState(candidates[2].Endpoint, candidates[2].Score);
                    _state3 = new CandidateState(candidates[3].Endpoint, candidates[3].Score);
                    break;

                default:
                    _state0 = new CandidateState(candidates[0].Endpoint, candidates[0].Score);
                    _state1 = new CandidateState(candidates[1].Endpoint, candidates[1].Score);
                    _state2 = new CandidateState(candidates[2].Endpoint, candidates[2].Score);
                    _state3 = new CandidateState(candidates[3].Endpoint, candidates[3].Score);

                    _additionalCandidates = new CandidateState[candidates.Length - 4];
                    for (var i = 4; i < candidates.Length; i++)
                    {
                        _additionalCandidates[i - 4] = new CandidateState(candidates[i].Endpoint, candidates[i].Score);
                    }
                    break;
            }

            // Initialize validity to valid by default.
            if (Count < BitVectorSize)
            {
                // Sets the bit for each candidate that exists (bits > Count will be 0).
                _validity = new BitVector32(unchecked((int)~(0xFFFFFFFFu << Count)));
            }
            else
            {
                _largeCapactityValidity = new BitArray(Count, defaultValue: true);
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

                switch (index)
                {
                    case 0:
                        return ref _state0;

                    case 1:
                        return ref _state1;

                    case 2:
                        return ref _state2;

                    case 3:
                        return ref _state3;

                    default:
                        return ref _additionalCandidates[index - 4];
                }
            }
        }

        /// <summary>
        /// Gets a value which indicates where the <see cref="Http.Endpoint"/> is considered
        /// a valid candiate for the current request.
        /// </summary>
        /// <param name="index">The candidate index.</param>
        /// <returns>
        /// <c>true</c> if the candidate at position <paramref name="index"/> is considered value
        /// for the current request, otherwise <c>false</c>.
        /// </returns>
        public bool IsValidCandidate(int index)
        {
            // Friendliness for inlining
            if ((uint)index >= Count)
            {
                ThrowIndexArgumentOutOfRangeException();
            }

            if (Count < BitVectorSize)
            {
                // Get the n-th bit
                return _validity[0x00000001 << index];
            }
            else
            {
                return _largeCapactityValidity[index];
            }
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

            if (Count < BitVectorSize)
            {
                // Set the n-th bit
                _validity[0x00000001 << index] = value;
            }
            else
            {
                _largeCapactityValidity[index] = value;
            }
        }

        private static void ThrowIndexArgumentOutOfRangeException()
        {
            throw new ArgumentOutOfRangeException("index");
        }
    }
}
