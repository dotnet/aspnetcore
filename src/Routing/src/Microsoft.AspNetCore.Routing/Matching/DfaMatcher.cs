// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing.Patterns;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Routing.Matching
{
    internal sealed class DfaMatcher : Matcher
    {
        private readonly ILogger _logger;
        private readonly EndpointSelector _selector;
        private readonly DfaState[] _states;
        private readonly int _maxSegmentCount;

        public DfaMatcher(ILogger<DfaMatcher> logger, EndpointSelector selector, DfaState[] states, int maxSegmentCount)
        {
            _logger = logger;
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

            // All of the logging we do here is at level debug, so we can get away with doing a single check.
            var log = _logger.IsEnabled(LogLevel.Debug);

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
            var (candidates, policies) = FindCandidateSet(httpContext, path, segments);
            if (candidates.Length == 0)
            {
                if (log)
                {
                    Logger.CandidatesNotFound(_logger, path);
                }

                return Task.CompletedTask;
            }

            if (log)
            {
                Logger.CandidatesFound(_logger, path, candidates);
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
                    if (!ProcessComplexSegments(candidate.Endpoint, candidate.ComplexSegments, path, segments, values))
                    {
                        candidateSet.SetValidity(i, false);
                        isMatch = false;
                    }
                }

                if ((flags & Candidate.CandidateFlags.HasConstraints) != 0)
                {
                    if (!ProcessConstraints(candidate.Endpoint, candidate.Constraints, httpContext, values))
                    {
                        candidateSet.SetValidity(i, false);
                        isMatch = false;
                    }
                }

                if (log)
                {
                    if (isMatch)
                    {
                        Logger.CandidateValid(_logger, path, candidate.Endpoint);
                    }
                    else
                    {
                        Logger.CandidateNotValid(_logger, path, candidate.Endpoint);
                    }
                }
            }

            if (policies.Length == 0)
            {
                // Perf: avoid a state machine if there are no polices
                return _selector.SelectAsync(httpContext, context, candidateSet);
            }

            return SelectEndpointWithPoliciesAsync(httpContext, context, policies, candidateSet);
        }

        internal (Candidate[] candidates, IEndpointSelectorPolicy[] policies) FindCandidateSet(
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

            return (states[destination].Candidates, states[destination].Policies);
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
            Endpoint endpoint,
            (RoutePatternPathSegment pathSegment, int segmentIndex)[] complexSegments,
            string path,
            ReadOnlySpan<PathSegment> segments,
            RouteValueDictionary values)
        {
            for (var i = 0; i < complexSegments.Length; i++)
            {
                (var complexSegment, var segmentIndex) = complexSegments[i];
                var segment = segments[segmentIndex];
                var text = path.Substring(segment.Start, segment.Length);
                if (!RoutePatternMatcher.MatchComplexSegment(complexSegment, text, values))
                {
                    Logger.CandidateRejectedByComplexSegment(_logger, path, endpoint, complexSegment);
                    return false;
                }
            }

            return true;
        }

        private bool ProcessConstraints(
            Endpoint endpoint,
            KeyValuePair<string, IRouteConstraint>[] constraints,
            HttpContext httpContext,
            RouteValueDictionary values)
        {
            for (var i = 0; i < constraints.Length; i++)
            {
                var constraint = constraints[i];
                if (!constraint.Value.Match(httpContext, NullRouter.Instance, constraint.Key, values, RouteDirection.IncomingRequest))
                {
                    Logger.CandidateRejectedByConstraint(_logger, httpContext.Request.Path, endpoint, constraint.Key, constraint.Value, values[constraint.Key]);
                    return false;
                }
            }

            return true;
        }

        private async Task SelectEndpointWithPoliciesAsync(
            HttpContext httpContext,
            EndpointSelectorContext context,
            IEndpointSelectorPolicy[] policies,
            CandidateSet candidateSet)
        {
            for (var i = 0; i < policies.Length; i++)
            {
                var policy = policies[i];
                await policy.ApplyAsync(httpContext, context, candidateSet);
                if (context.Endpoint != null)
                {
                    // This is a short circuit, the selector chose an endpoint.
                    return;
                }
            }

            await _selector.SelectAsync(httpContext, context, candidateSet);
        }

        internal static class EventIds
        {
            public static readonly EventId CandidatesNotFound = new EventId(1000, "CandidatesNotFound");
            public static readonly EventId CandidatesFound = new EventId(1001, "CandidatesFound");

            public static readonly EventId CandidateRejectedByComplexSegment = new EventId(1002, "CandidateRejectedByComplexSegment");
            public static readonly EventId CandidateRejectedByConstraint = new EventId(1003, "CandidateRejectedByConstraint");

            public static readonly EventId CandidateNotValid = new EventId(1004, "CandiateNotValid");
            public static readonly EventId CandidateValid = new EventId(1005, "CandiateValid");
        }

        private static class Logger
        {
            private static readonly Action<ILogger, string, Exception> _candidatesNotFound = LoggerMessage.Define<string>(
                LogLevel.Debug,
                EventIds.CandidatesNotFound,
                "No candidates found for the request path '{Path}'");

            private static readonly Action<ILogger, int, string, Exception> _candidatesFound = LoggerMessage.Define<int, string>(
                LogLevel.Debug,
                EventIds.CandidatesFound,
                "{CandidateCount} candidate(s) found for the request path '{Path}'");

            private static readonly Action<ILogger, string, string, string, string, Exception> _candidateRejectedByComplexSegment = LoggerMessage.Define<string, string, string, string>(
                LogLevel.Debug,
                EventIds.CandidateRejectedByComplexSegment,
                "Endpoint '{Endpoint}' with route pattern '{RoutePattern}' was rejected by complex segment '{Segment}' for the request path '{Path}'");

            private static readonly Action<ILogger, string, string, string, string, object, string, Exception> _candidateRejectedByConstraint = LoggerMessage.Define<string, string, string, string, object, string>(
                LogLevel.Debug,
                EventIds.CandidateRejectedByConstraint,
                "Endpoint '{Endpoint}' with route pattern '{RoutePattern}' was rejected by constraint '{ConstraintName}':'{Constraint}' with value '{RouteValue}' for the request path '{Path}'");

            private static readonly Action<ILogger, string, string, string, Exception> _candidateNotValid = LoggerMessage.Define<string, string, string>(
                LogLevel.Debug,
                EventIds.CandidateNotValid,
                "Endpoint '{Endpoint}' with route pattern '{RoutePattern}' is not valid for the request path '{Path}'");

            private static readonly Action<ILogger, string, string, string, Exception> _candidateValid = LoggerMessage.Define<string, string, string>(
                LogLevel.Debug,
                EventIds.CandidateValid,
                "Endpoint '{Endpoint}' with route pattern '{RoutePattern}' is valid for the request path '{Path}'");

            public static void CandidatesNotFound(ILogger logger, string path)
            {
                _candidatesNotFound(logger, path, null);
            }

            public static void CandidatesFound(ILogger logger, string path, Candidate[] candidates)
            {
                _candidatesFound(logger, candidates.Length, path, null);
            }

            public static void CandidateRejectedByComplexSegment(ILogger logger, string path, Endpoint endpoint, RoutePatternPathSegment segment)
            {
                // This should return a real pattern since we're processing complex segments.... but just in case.
                if (logger.IsEnabled(LogLevel.Debug))
                {
                    var routePattern = GetRoutePattern(endpoint);
                    _candidateRejectedByComplexSegment(logger, endpoint.DisplayName, routePattern, segment.DebuggerToString(), path, null);
                }
            }

            public static void CandidateRejectedByConstraint(ILogger logger, string path, Endpoint endpoint, string constraintName, IRouteConstraint constraint, object value)
            {
                // This should return a real pattern since we're processing constraints.... but just in case.
                if (logger.IsEnabled(LogLevel.Debug))
                {
                    var routePattern = GetRoutePattern(endpoint);
                    _candidateRejectedByConstraint(logger, endpoint.DisplayName, routePattern, constraintName, constraint.ToString(), value, path, null);
                }
            }

            public static void CandidateNotValid(ILogger logger, string path, Endpoint endpoint)
            {
                // This can be the fallback value because it really might not be a route endpoint
                if (logger.IsEnabled(LogLevel.Debug))
                {
                    var routePattern = GetRoutePattern(endpoint);
                    _candidateNotValid(logger, endpoint.DisplayName, routePattern, path, null);
                }
            }

            public static void CandidateValid(ILogger logger, string path, Endpoint endpoint)
            {
                // This can be the fallback value because it really might not be a route endpoint
                if (logger.IsEnabled(LogLevel.Debug))
                {
                    var routePattern = GetRoutePattern(endpoint);
                    _candidateValid(logger, endpoint.DisplayName, routePattern, path, null);
                }
            }

            private static string GetRoutePattern(Endpoint endpoint)
            {
                return (endpoint as RouteEndpoint)?.RoutePattern?.RawText ?? "(none)";
            }
        }
    }
}
