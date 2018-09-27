// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Routing.Matching
{
    internal class DefaultEndpointSelector : EndpointSelector
    {
        private readonly IEndpointSelectorPolicy[] _selectorPolicies;
        
        public DefaultEndpointSelector(IEnumerable<MatcherPolicy> matcherPolicies)
        {
            if (matcherPolicies == null)
            {
                throw new ArgumentNullException(nameof(matcherPolicies));
            }

            _selectorPolicies = matcherPolicies.OrderBy(p => p.Order).OfType<IEndpointSelectorPolicy>().ToArray();
        }

        public override async Task SelectAsync(
            HttpContext httpContext,
            EndpointSelectorContext context,
            CandidateSet candidateSet)
        {
            var selectorPolicies = _selectorPolicies;
            for (var i = 0; i < _selectorPolicies.Length; i++)
            {
                await selectorPolicies[i].ApplyAsync(httpContext, context, candidateSet);
                if (context.Endpoint != null)
                {
                    // This is a short circuit, the selector chose an endpoint.
                    return;
                }
            }

            ProcessFinalCandidates(httpContext, context, candidateSet);
        }

        private static void ProcessFinalCandidates(
            HttpContext httpContext,
            EndpointSelectorContext context,
            CandidateSet candidateSet)
        {
            Endpoint endpoint = null;
            RouteValueDictionary values = null;
            int? foundScore = null;
            for (var i = 0; i < candidateSet.Count; i++)
            {
                ref var state = ref candidateSet[i];

                var isValid = state.IsValidCandidate;
                if (isValid && foundScore == null)
                {
                    // This is the first match we've seen - speculatively assign it.
                    endpoint = state.Endpoint;
                    values = state.Values;
                    foundScore = state.Score;
                }
                else if (isValid && foundScore < state.Score)
                {
                    // This candidate is lower priority than the one we've seen
                    // so far, we can stop.
                    //
                    // Don't worry about the 'null < state.Score' case, it returns false.
                    break;
                }
                else if (isValid && foundScore == state.Score)
                {
                    // This is the second match we've found of the same score, so there 
                    // must be an ambiguity.
                    //
                    // Don't worry about the 'null == state.Score' case, it returns false.

                    ReportAmbiguity(candidateSet);

                    // Unreachable, ReportAmbiguity always throws.
                    throw new NotSupportedException();
                }
            }

            if (endpoint != null)
            {
                context.Endpoint = endpoint;
                context.RouteValues = values;
            }
        }

        private static void ReportAmbiguity(CandidateSet candidates)
        {
            // If we get here it's the result of an ambiguity - we're OK with this
            // being a littler slower and more allocatey.
            var matches = new List<Endpoint>();
            for (var i = 0; i < candidates.Count; i++)
            {
                ref var state = ref candidates[i];
                if (state.IsValidCandidate)
                {
                    matches.Add(state.Endpoint);
                }
            }

            var message = Resources.FormatAmbiguousEndpoints(
                Environment.NewLine,
                string.Join(Environment.NewLine, matches.Select(e => e.DisplayName)));
            throw new AmbiguousMatchException(message);
        }
    }
}
