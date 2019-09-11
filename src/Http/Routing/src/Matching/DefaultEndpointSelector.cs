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
        public override Task SelectAsync(
            HttpContext httpContext,
            EndpointSelectorContext context,
            CandidateSet candidateSet)
        {
            if (httpContext == null)
            {
                throw new ArgumentNullException(nameof(httpContext));
            }

            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            if (candidateSet == null)
            {
                throw new ArgumentNullException(nameof(candidateSet));
            }

            // Fast path: We can specialize for trivial numbers of candidates since there can
            // be no ambiguities
            switch (candidateSet.Count)
            {
                case 0:
                    {
                        // Do nothing
                        break;
                    }

                case 1:
                    {
                        if (candidateSet.IsValidCandidate(0))
                        {
                            ref var state = ref candidateSet[0];
                            context.Endpoint = state.Endpoint;
                            context.RouteValues = state.Values;
                        }

                        break;
                    }

                default:
                    {
                        // Slow path: There's more than one candidate (to say nothing of validity) so we
                        // have to process for ambiguities.
                        ProcessFinalCandidates(httpContext, context, candidateSet);
                        break;
                    }
            }

            return Task.CompletedTask;
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
                if (!candidateSet.IsValidCandidate(i))
                {
                    continue;
                }

                ref var state = ref candidateSet[i];
                if (foundScore == null)
                {
                    // This is the first match we've seen - speculatively assign it.
                    endpoint = state.Endpoint;
                    values = state.Values;
                    foundScore = state.Score;
                }
                else if (foundScore < state.Score)
                {
                    // This candidate is lower priority than the one we've seen
                    // so far, we can stop.
                    //
                    // Don't worry about the 'null < state.Score' case, it returns false.
                    break;
                }
                else if (foundScore == state.Score)
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
                if (candidates.IsValidCandidate(i))
                {
                    matches.Add(candidates[i].Endpoint);
                }
            }

            var message = Resources.FormatAmbiguousEndpoints(
                Environment.NewLine,
                string.Join(Environment.NewLine, matches.Select(e => e.DisplayName)));
            throw new AmbiguousMatchException(message);
        }
    }
}
