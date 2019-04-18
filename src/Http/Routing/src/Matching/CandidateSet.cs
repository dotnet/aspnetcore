// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Buffers;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Linq;
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

        private CandidateState[] _candidates;

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

            _candidates = new CandidateState[endpoints.Length];
            for (var i = 0; i < endpoints.Length; i++)
            {
                _candidates[i] = new CandidateState(endpoints[i], values[i], scores[i]);
            }
        }

        internal CandidateSet(Candidate[] candidates)
        {
            _candidates = new CandidateState[candidates.Length];
            for (var i = 0; i < candidates.Length; i++)
            {
                _candidates[i] = new CandidateState(candidates[i].Endpoint, candidates[i].Score);
            }
        }

        /// <summary>
        /// Gets the count of candidates in the set.
        /// </summary>
        public int Count => _candidates.Length;

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
        /// the <paramref name="index"/>. If <paramref name="endpoint"/> is <c>null</c>. the candidate will be marked
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

        /// <summary>
        /// Replaces the <see cref="Endpoint"/> at the provided <paramref name="index"/> with the
        /// provided <paramref name="endpoints"/>.
        /// </summary>
        /// <param name="index">The candidate index.</param>
        /// <param name="endpoints">
        /// The list of endpoints <see cref="Endpoint"/> to replace the original <see cref="Endpoint"/> at
        /// the <paramref name="index"/>. If <paramref name="endpoints"/> is empty, the candidate will be marked
        /// as invalid.
        /// </param>
        /// <param name="comparer">
        /// The endpoint comparer used to order the endpoints. Can be retrieved from the service provider as
        /// type <see cref="EndpointMetadataComparer"/>.
        /// </param>
        /// <remarks>
        /// <para>
        /// This method supports replacing a dynamic endpoint with a collection of endpoints, and relying on
        /// <see cref="IEndpointSelectorPolicy"/> implementations to disambiguate further.
        /// </para>
        /// <para>
        /// The endpoint being replace should have a unique score value. The score is the combination of route
        /// patter precedence, order, and policy metadata evaluation. A dynamic endpoint will not function
        /// correctly if other endpoints exist with the same score.
        /// </para>
        /// </remarks>
        public void ExpandEndpoint(int index, IReadOnlyList<Endpoint> endpoints, IComparer<Endpoint> comparer)
        {
            // Friendliness for inlining
            if ((uint)index >= Count)
            {
                ThrowIndexArgumentOutOfRangeException();
            }

            if (endpoints == null)
            {
                ThrowArgumentNullException(nameof(endpoints));
            }

            if (comparer == null)
            {
                ThrowArgumentNullException(nameof(comparer));
            }

            // First we need to verify that the score of what we're replacing is unique.
            ValidateUniqueScore(index);

            switch (endpoints.Count)
            {
                case 0:
                    ReplaceEndpoint(index, null, null);
                    break;

                case 1:
                    ReplaceEndpoint(index, endpoints[0], _candidates[index].Values);
                    break;

                default:

                    var score = GetOriginalScore(index);
                    var values = _candidates[index].Values;

                    // Adding candidates requires expanding the array and computing new score values for the new candidates.
                    var original = _candidates;
                    var candidates = new CandidateState[original.Length - 1 + endpoints.Count];
                    _candidates = candidates;

                    // Since the new endpoints have an unknown ordering relationship to each other, we need to:
                    // - order them
                    // - assign scores
                    // - offset everything that comes after
                    //
                    // If the inputs look like:
                    //
                    // score 0: A1
                    // score 0: A2
                    // score 1: B
                    // score 2: C <-- being expanded
                    // score 3: D
                    //
                    // Then the result should look like:
                    //
                    // score 0: A1
                    // score 0: A2
                    // score 1: B
                    // score 2: `C1
                    // score 3: `C2
                    // score 4: D

                    // Candidates before index can be copied unchanged.
                    for (var i = 0; i < index; i++)
                    {
                        candidates[i] = original[i];
                    }

                    var buffer = endpoints.ToArray();
                    Array.Sort<Endpoint>(buffer, comparer);

                    // Add the first new endpoint with the current score
                    candidates[index] = new CandidateState(buffer[0], values, score);

                    var scoreOffset = 0;
                    for (var i = 1; i < buffer.Length; i++)
                    {
                        var cmp = comparer.Compare(buffer[i - 1], buffer[i]);

                        // This should not be possible. This would mean that sorting is wrong.
                        Debug.Assert(cmp <= 0);
                        if (cmp == 0)
                        {
                            // Score is unchanged.
                        }
                        else if (cmp < 0)
                        {
                            // Endpoint is lower priority, higher score.
                            scoreOffset++;
                        }

                        _candidates[i + index] = new CandidateState(buffer[i], values, score + scoreOffset);
                    }

                    for (var i = index + 1; i < original.Length; i++)
                    {
                        _candidates[i + endpoints.Count - 1] = new CandidateState(original[i].Endpoint, original[i].Values, original[i].Score + scoreOffset);
                    }

                    break;
                    
            }
        }

        // Returns the *positive* score value. Score is used to track valid/invalid which can cause it to be negative.
        //
        // This is the original score and used to determine if there are ambiguities.
        private int GetOriginalScore(int index)
        {
            var score = _candidates[index].Score;
            return score >= 0 ? score : ~score;
        }

        private void ValidateUniqueScore(int index)
        {
            var score = GetOriginalScore(index);

            var count = 0;
            var candidates = _candidates;
            for (var i = 0; i < candidates.Length; i++)
            {
                if (GetOriginalScore(i) == score)
                {
                    count++;
                }
            }

            Debug.Assert(count > 0);
            if (count > 1)
            {
                // Uh-oh. We don't allow duplicates with ExpandEndpoint because that will do unpredictable things.
                var duplicates = new List<Endpoint>();
                for (var i = 0; i < candidates.Length; i++)
                {
                    if (GetOriginalScore(i) == score)
                    {
                        duplicates.Add(candidates[i].Endpoint);
                    }
                }

                var message =
                    $"Using {nameof(ExpandEndpoint)} requires that the replaced endpoint have a unique priority. " +
                    $"The following endpoints were found with the same priority:" + Environment.NewLine +
                    string.Join(Environment.NewLine, duplicates.Select(e => e.DisplayName));
                throw new InvalidOperationException(message);
            }
        }

        private static void ThrowIndexArgumentOutOfRangeException()
        {
            throw new ArgumentOutOfRangeException("index");
        }

        private static void ThrowArgumentNullException(string parameter)
        {
            throw new ArgumentNullException(parameter);
        }
    }
}
