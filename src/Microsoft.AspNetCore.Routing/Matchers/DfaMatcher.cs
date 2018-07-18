// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing.EndpointConstraints;
using Microsoft.AspNetCore.Routing.Patterns;

namespace Microsoft.AspNetCore.Routing.Matchers
{
    internal sealed class DfaMatcher : Matcher
    {
        private readonly EndpointSelector _endpointSelector;
        private readonly DfaState[] _states;

        public DfaMatcher(EndpointSelector endpointSelector, DfaState[] states)
        {
            _endpointSelector = endpointSelector;
            _states = states;
        }

        public sealed override Task MatchAsync(HttpContext httpContext, IEndpointFeature feature)
        {
            if (httpContext == null)
            {
                throw new ArgumentNullException(nameof(httpContext));
            }

            if (feature == null)
            {
                throw new ArgumentNullException(nameof(feature));
            }

            // The sequence of actions we take is optimized to avoid doing expensive work
            // like creating substrings, creating route value dictionaries, and calling
            // into policies like versioning.
            var path = httpContext.Request.Path.Value;

            // First tokenize the path into series of segments.
            Span<PathSegment> buffer = stackalloc PathSegment[FastPathTokenizer.DefaultSegmentCount];
            var count = FastPathTokenizer.Tokenize(path, buffer);
            var segments = buffer.Slice(0, count);

            // SelectCandidates will process the DFA and return a candidate set. This does
            // some preliminary matching of the URL (mostly the literal segments).
            var candidates = SelectCandidates(path, segments);
            if (candidates.GroupCount == 0)
            {
                return Task.CompletedTask;
            }

            // At this point we have a candidate set, defined as a list of groups
            // of candidates. Each member of a given group has the same priority
            // (priority is defined by order, precedence and other factors like http method
            // or version).
            //
            // We don't yet know that any candidate can be considered a match, because
            // we haven't processed things like route constraints and complex segments.
            //
            // Now we'll go group by group to capture route values, process constraints,
            // and process complex segments.
            //
            // Perf: using groups.Length - 1 here to elide the bounds check. We're relying
            // on assumptions of how Groups works.
            var candidatesArray = candidates.Candidates;
            var groups = candidates.Groups;

            for (var i = 0; i < groups.Length - 1; i++)
            {
                var start = groups[i];
                var length = groups[i + 1] - groups[i];
                var group = candidatesArray.AsSpan(start, length);

                // Yes, these allocate. We should revise how this interaction works exactly
                // once the extensibility is more locked down.
                //
                // Would could produce a fast path for a small number of members in
                // a group.
                var members = new BitArray(group.Length);
                var groupValues = new RouteValueDictionary[group.Length];

                if (FilterGroup(
                    httpContext,
                    path,
                    segments,
                    group,
                    members,
                    groupValues))
                {
                    // We must have some matches because FilterGroup returned true.

                    // So: this code is SUPER SUPER temporary. We don't intent to keep
                    // EndpointSelector around for very long.
                    var candidatesForEndpointSelector = new List<Endpoint>();
                    for (var j = 0; j < group.Length; j++)
                    {
                        if (members.Get(j))
                        {
                            candidatesForEndpointSelector.Add(group[j].Endpoint);
                        }
                    }

                    var result = _endpointSelector.SelectBestCandidate(httpContext, candidatesForEndpointSelector);
                    if (result != null)
                    {
                        // Find the route values, based on which endpoint was selected. We have
                        // to do this because the endpoint selector returns an endpoint
                        // instead of mutating the feature.
                        for (var j = 0; j < group.Length; j++)
                        {
                            if (ReferenceEquals(result, group[j].Endpoint))
                            {
                                feature.Endpoint = result;
                                feature.Invoker = ((MatcherEndpoint)result).Invoker;
                                feature.Values = groupValues[j];
                                return Task.CompletedTask;
                            }
                        }
                    }

                    // End super temporary code
                }
            }

            return Task.CompletedTask;
        }

        internal CandidateSet SelectCandidates(string path, ReadOnlySpan<PathSegment> segments)
        {
            var states = _states;

            var destination = 0;
            for (var i = 0; i < segments.Length; i++)
            {
                destination = states[destination].Transitions.GetDestination(path, segments[i]);
            }

            return states[destination].Candidates;
        }

        private bool FilterGroup(
            HttpContext httpContext,
            string path,
            ReadOnlySpan<PathSegment> segments,
            ReadOnlySpan<Candidate> group,
            BitArray members,
            RouteValueDictionary[] groupValues)
        {
            var hasMatch = false;
            for (var i = 0; i < group.Length; i++)
            {
                // PERF: specifically not copying group[i] into a local. It's a relatively
                // fat struct and we don't want to eagerly copy it.
                var flags = group[i].Flags;

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
                    var prototype = group[i].Slots;
                    var slots = new KeyValuePair<string, object>[prototype.Length];

                    if ((flags & Candidate.CandidateFlags.HasDefaults) != 0)
                    {
                        Array.Copy(prototype, 0, slots, 0, prototype.Length);
                    }

                    if ((flags & Candidate.CandidateFlags.HasCaptures) != 0)
                    {
                        ProcessCaptures(slots, group[i].Captures, path, segments);
                    }

                    if ((flags & Candidate.CandidateFlags.HasCatchAll) != 0)
                    { 
                        ProcessCatchAll(slots, group[i].CatchAll, path, segments);
                    }

                    values = RouteValueDictionary.FromArray(slots);
                }

                groupValues[i] = values;
                
                // Now that we have the route values, we need to process complex segments.
                // Complex segments go through an old API that requires a fully-materialized
                // route value dictionary.
                var isMatch = true;
                if ((flags & Candidate.CandidateFlags.HasComplexSegments) != 0)
                {
                    isMatch &= ProcessComplexSegments(group[i].ComplexSegments, path, segments, values);
                }

                if ((flags & Candidate.CandidateFlags.HasMatchProcessors) != 0)
                {
                    isMatch &= ProcessMatchProcessors(group[i].MatchProcessors, httpContext, values);
                }

                members.Set(i, isMatch);
                hasMatch |= isMatch;
            }

            return hasMatch;
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

        private bool ProcessMatchProcessors(
            MatchProcessor[] matchProcessors,
            HttpContext httpContext,
            RouteValueDictionary values)
        {
            for (var i = 0; i < matchProcessors.Length; i++)
            {
                var matchProcessor = matchProcessors[i];
                if (!matchProcessor.ProcessInbound(httpContext, values))
                {
                    return false;
                }
            }

            return true;
        }
    }
}
