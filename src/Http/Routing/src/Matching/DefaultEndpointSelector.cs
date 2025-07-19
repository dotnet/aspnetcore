// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq;
using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Routing.Matching;

internal sealed class DefaultEndpointSelector : EndpointSelector
{
    public override Task SelectAsync(
        HttpContext httpContext,
        CandidateSet candidateSet)
    {
        ArgumentNullException.ThrowIfNull(httpContext);
        ArgumentNullException.ThrowIfNull(candidateSet);

        Select(httpContext, candidateSet.Candidates);
        return Task.CompletedTask;
    }

    internal static void Select(HttpContext httpContext, Span<CandidateState> candidateState)
    {
        // Fast path: We can specialize for trivial numbers of candidates since there can
        // be no ambiguities
        switch (candidateState.Length)
        {
            case 0:
                {
                    // Do nothing
                    break;
                }

            case 1:
                {
                    ref var state = ref candidateState[0];
                    if (CandidateSet.IsValidCandidate(ref state))
                    {
                        httpContext.SetEndpoint(state.Endpoint);
                        httpContext.Request.RouteValues = state.Values!;
                    }

                    break;
                }

            default:
                {
                    // Slow path: There's more than one candidate (to say nothing of validity) so we
                    // have to process for ambiguities.
                    ProcessFinalCandidates(httpContext, candidateState);
                    break;
                }
        }
    }

    private static void ProcessFinalCandidates(
        HttpContext httpContext,
        Span<CandidateState> candidateState)
    {
        Endpoint? endpoint = null;
        RouteValueDictionary? values = null;
        int? foundScore = null;
        var candidatesWithSameScore = new List<CandidateState>();

        for (var i = 0; i < candidateState.Length; i++)
        {
            ref var state = ref candidateState[i];
            if (!CandidateSet.IsValidCandidate(ref state))
            {
                continue;
            }

            if (foundScore == null)
            {
                // This is the first match we've seen - speculatively assign it.
                endpoint = state.Endpoint;
                values = state.Values;
                foundScore = state.Score;
                candidatesWithSameScore.Add(state);
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
                // Same score - collect for constraint specificity analysis. We cant
                // just dismiss these candidate and report an ambiguity, we need to
                // analyze them for constraint specificity.
                candidatesWithSameScore.Add(state);
            }
        }

        // If we have multiple candidates with the same score, try to resolve using
        // constraint specificity rules
        if (candidatesWithSameScore.Count > 1)
        {
            var mostSpecific = SelectMostSpecificEndpoint(candidatesWithSameScore);
            if (mostSpecific.HasValue)
            {
                endpoint = mostSpecific.Value.Endpoint;
                values = mostSpecific.Value.Values;
            }
            else
            {
                // Still ambiguous after constraint analysis
                ReportAmbiguity(candidateState);
            }
        }

        if (endpoint != null)
        {
            httpContext.SetEndpoint(endpoint);
            httpContext.Request.RouteValues = values!;
        }
    }

    private static CandidateState? SelectMostSpecificEndpoint(List<CandidateState> candidates)
    {
        CandidateState? mostSpecific = null;
        var highestSpecificity = -1;
        var hasAmbiguity = false;

        foreach (var candidate in candidates)
        {
            if (candidate.Endpoint is not RouteEndpoint routeEndpoint)
            {
                continue;
            }

            var specificity = CalculateConstraintSpecificity(routeEndpoint);

            if (specificity > highestSpecificity)
            {
                highestSpecificity = specificity;
                mostSpecific = candidate;
                hasAmbiguity = false;
            }
            else if (specificity == highestSpecificity)
            {
                // Okay, note the ambiguity and continue trying
                // to determine a higher level of specificity.
                hasAmbiguity = true;
            }
        }

        return hasAmbiguity ? null : mostSpecific;
    }

    private static int CalculateConstraintSpecificity(RouteEndpoint endpoint)
    {
        var specificity = 0;
        var routePattern = endpoint.RoutePattern;

        foreach (var parameter in routePattern.Parameters)
        {
            // We may have parameter without constraints, e.g. "id" in "/products/{id}"
            if (parameter.ParameterPolicies?.Count > 0)
            {
                foreach (var policy in parameter.ParameterPolicies)
                {
                    if (policy.Content != null)
                    {
                        specificity += GetConstraintSpecificityWeight(policy.Content);
                    }
                }
            }
        }

        return specificity;
    }

    private static int GetConstraintSpecificityWeight(string constraintName)
    {
        return constraintName.ToLowerInvariant() switch
        {
            // Strong typed constraints that are very restrictive and has
            // the highest specificity
            "guid" => 100,
            "datetime" => 90,
            "decimal" => 85,
            "double" => 80,
            "float" => 75,
            "long" => 70,
            "int" => 65,
            "bool" => 60,

            // Range constraint are more restrictive than other types
            var range when range.StartsWith("range(", StringComparison.OrdinalIgnoreCase) => 55,
            var min when min.StartsWith("min(", StringComparison.OrdinalIgnoreCase) => 50,
            var max when max.StartsWith("max(", StringComparison.OrdinalIgnoreCase) => 50,

            // This one is a bit odd, but we will consider it less specific than range,
            // since it defines only the length of the value and will consider it as raw
            // string not a number.
            var length when length.StartsWith("length(", StringComparison.OrdinalIgnoreCase) => 45,
            var minlength when minlength.StartsWith("minlength(", StringComparison.OrdinalIgnoreCase) => 40,
            var maxlength when maxlength.StartsWith("maxlength(", StringComparison.OrdinalIgnoreCase) => 40,

            // String patterns, which are less specific than length
            "alpha" => 35,
            var regex when regex.StartsWith("regex(", StringComparison.OrdinalIgnoreCase) => 30,

            // File constraints
            "file" => 25,
            "nonfile" => 25,

            // Least specific just requires non empty
            "required" => 10,

            // Unknown constraint, assign medium specificity to it
            _ => 20
        };
    }

    private static void ReportAmbiguity(Span<CandidateState> candidateState)
    {
        // If we get here it's the result of an ambiguity - we're OK with this
        // being a littler slower and more allocatey.
        var matches = new List<Endpoint>();
        for (var i = 0; i < candidateState.Length; i++)
        {
            ref var state = ref candidateState[i];
            if (CandidateSet.IsValidCandidate(ref state))
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
