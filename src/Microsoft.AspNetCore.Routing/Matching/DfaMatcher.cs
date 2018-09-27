// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Routing.Patterns;

namespace Microsoft.AspNetCore.Routing.Matching
{
    internal sealed class DfaMatcher : Matcher
    {
        private readonly EndpointSelector _selector;
        private readonly DfaState[] _states;
        private readonly int _maxSegmentCount;
        
        public DfaMatcher(EndpointSelector selector, DfaState[] states, int maxSegmentCount)
        {
            _selector = selector;
            _states = states;
            _maxSegmentCount = maxSegmentCount;
        }

        public sealed override Task MatchAsync(HttpContext httpContext, EndpointSelectorContext context)
        {
            if (httpContext == null)
            {
                throw new ArgumentNullException(nameof(httpContext));
            }

            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            // The sequence of actions we take is optimized to avoid doing expensive work
            // like creating substrings, creating route value dictionaries, and calling
            // into policies like versioning.
            var path = httpContext.Request.Path.Value;

            // First tokenize the path into series of segments.
            Span<PathSegment> buffer = stackalloc PathSegment[_maxSegmentCount];
            var count = FastPathTokenizer.Tokenize(path, buffer);
            var segments = buffer.Slice(0, count);

            // FindCandidateSet will process the DFA and return a candidate set. This does
            // some preliminary matching of the URL (mostly the literal segments).
            var candidates = FindCandidateSet(httpContext, path, segments);
            if (candidates.Length == 0)
            {
                return Task.CompletedTask;
            }

            // At this point we have a candidate set, defined as a list of endpoints in
            // priority order.
            //
            // We don't yet know that any candidate can be considered a match, because
            // we haven't processed things like route constraints and complex segments.
            //
            // Now we'll iterate each endpoint to capture route values, process constraints,
            // and process complex segments.

            // `candidates` has all of our internal state that we use to process the
            // set of endpoints before we call the EndpointSelector.
            //
            // `candidateSet` is the mutable state that we pass to the EndpointSelector.
            var candidateSet = new CandidateSet(candidates);

            for (var i = 0; i < candidates.Length; i++)
            {
                // PERF: using ref here to avoid copying around big structs.
                //
                // Reminder!
                // candidate: readonly data about the endpoint and how to match
                // state: mutable storarge for our processing
                ref var candidate = ref candidates[i];
                ref var state = ref candidateSet[i];

                var flags = candidate.Flags;

                // First process all of the parameters and defaults.
                RouteValueDictionary values;
                if ((flags & Candidate.CandidateFlags.HasSlots) == 0)
                {
                    values = new RouteValueDictionary();
                }
                else
                {
                    // The Slots array has the default values of the route values in it.
                    //
                    // We want to create a new array for the route values based on Slots
                    // as a prototype.
                    var prototype = candidate.Slots;
                    var slots = new KeyValuePair<string, object>[prototype.Length];

                    if ((flags & Candidate.CandidateFlags.HasDefaults) != 0)
                    {
                        Array.Copy(prototype, 0, slots, 0, prototype.Length);
                    }

                    if ((flags & Candidate.CandidateFlags.HasCaptures) != 0)
                    {
                        ProcessCaptures(slots, candidate.Captures, path, segments);
                    }

                    if ((flags & Candidate.CandidateFlags.HasCatchAll) != 0)
                    {
                        ProcessCatchAll(slots, candidate.CatchAll, path, segments);
                    }

                    values = RouteValueDictionary.FromArray(slots);
                }

                state.Values = values;

                // Now that we have the route values, we need to process complex segments.
                // Complex segments go through an old API that requires a fully-materialized
                // route value dictionary.
                var isMatch = true;
                if ((flags & Candidate.CandidateFlags.HasComplexSegments) != 0)
                {
                    isMatch &= ProcessComplexSegments(candidate.ComplexSegments, path, segments, values);
                }

                if ((flags & Candidate.CandidateFlags.HasConstraints) != 0)
                {
                    isMatch &= ProcessConstraints(candidate.Constraints, httpContext, values);
                }

                state.IsValidCandidate = isMatch;
            }

            return _selector.SelectAsync(httpContext, context, candidateSet);
        }

        internal Candidate[] FindCandidateSet(
            HttpContext httpContext,
            string path,
            ReadOnlySpan<PathSegment> segments)
        {
            var states = _states;

            // Process each path segment
            var destination = 0;
            for (var i = 0; i < segments.Length; i++)
            {
                destination = states[destination].PathTransitions.GetDestination(path, segments[i]);
            }

            // Process an arbitrary number of policy-based decisions
            var policyTransitions = states[destination].PolicyTransitions;
            while (policyTransitions != null)
            {
                destination = policyTransitions.GetDestination(httpContext);
                policyTransitions = states[destination].PolicyTransitions;
            }

            return states[destination].Candidates;
        }

        private void ProcessCaptures(
            KeyValuePair<string, object>[] slots,
            (string parameterName, int segmentIndex, int slotIndex)[] captures,
            string path,
            ReadOnlySpan<PathSegment> segments)
        {
            for (var i = 0; i < captures.Length; i++)
            {
                var parameterName = captures[i].parameterName;
                if (segments.Length > captures[i].segmentIndex)
                {
                    var segment = segments[captures[i].segmentIndex];
                    if (parameterName != null && segment.Length > 0)
                    {
                        slots[captures[i].slotIndex] = new KeyValuePair<string, object>(
                            parameterName,
                            path.Substring(segment.Start, segment.Length));
                    }
                }
            }
        }

        private void ProcessCatchAll(
            KeyValuePair<string, object>[] slots,
            (string parameterName, int segmentIndex, int slotIndex) catchAll,
            string path,
            ReadOnlySpan<PathSegment> segments)
        {
            if (segments.Length > catchAll.segmentIndex)
            {
                var segment = segments[catchAll.segmentIndex];
                slots[catchAll.slotIndex] = new KeyValuePair<string, object>(
                    catchAll.parameterName,
                    path.Substring(segment.Start));
            }
        }

        private bool ProcessComplexSegments(
            (RoutePatternPathSegment pathSegment, int segmentIndex)[] complexSegments,
            string path,
            ReadOnlySpan<PathSegment> segments,
            RouteValueDictionary values)
        {
            for (var i = 0; i < complexSegments.Length; i++)
            {
                var segment = segments[complexSegments[i].segmentIndex];
                var text = path.Substring(segment.Start, segment.Length);
                if (!RoutePatternMatcher.MatchComplexSegment(complexSegments[i].pathSegment, text, values))
                {
                    return false;
                }
            }

            return true;
        }

        private bool ProcessConstraints(
            KeyValuePair<string, IRouteConstraint>[] constraints,
            HttpContext httpContext,
            RouteValueDictionary values)
        {
            for (var i = 0; i < constraints.Length; i++)
            {
                var constraint = constraints[i];
                if (!constraint.Value.Match(httpContext, NullRouter.Instance, constraint.Key, values, RouteDirection.IncomingRequest))
                {
                    return false;
                }
            }

            return true;
        }
    }
}
