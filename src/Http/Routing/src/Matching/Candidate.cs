// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing.Patterns;

namespace Microsoft.AspNetCore.Routing.Matching;

internal readonly struct Candidate
{
    public readonly Endpoint Endpoint;

    // Used to optimize out operations that modify route values.
    public readonly CandidateFlags Flags;

    // Data for creating the RouteValueDictionary. We assign each key its own slot
    // and we fill the values array with all of the default values.
    //
    // Then when we process parameters, we don't need to operate on the RouteValueDictionary
    // we can just operate on an array, which is much much faster.
    public readonly KeyValuePair<string, object>[] Slots;

    // List of parameters to capture. Segment is the segment index, index is the
    // index into the values array.
    public readonly (string parameterName, int segmentIndex, int slotIndex)[] Captures;

    // Catchall parameter to capture (limit one per template).
    public readonly (string parameterName, int segmentIndex, int slotIndex) CatchAll;

    // Complex segments are processed in a separate pass because they require a
    // RouteValueDictionary.
    public readonly (RoutePatternPathSegment pathSegment, int segmentIndex)[] ComplexSegments;

    public readonly KeyValuePair<string, IRouteConstraint>[] Constraints;

    // Score is a sequential integer value that in determines the priority of an Endpoint.
    // Scores are computed within the context of candidate set, and are meaningless when
    // applied to endpoints not in the set.
    //
    // The score concept boils down the system of comparisons done when ordering Endpoints
    // to a single value that can be compared easily. This can be defeated by having
    // int32.MaxValue + 1 endpoints in a single set, but you would have other problems by
    // that point.
    //
    // Score is not part of the Endpoint itself, because it's contextual based on where
    // the endpoint appears. An Endpoint is often be a member of multiple candidate sets.
    public readonly int Score;

    // Used in tests.
    public Candidate(Endpoint endpoint)
    {
        Endpoint = endpoint;

        Slots = Array.Empty<KeyValuePair<string, object>>();
        Captures = Array.Empty<(string parameterName, int segmentIndex, int slotIndex)>();
        CatchAll = default;
        ComplexSegments = Array.Empty<(RoutePatternPathSegment pathSegment, int segmentIndex)>();
        Constraints = Array.Empty<KeyValuePair<string, IRouteConstraint>>();
        Score = 0;

        Flags = CandidateFlags.None;
    }

    public Candidate(
        Endpoint endpoint,
        int score,
        KeyValuePair<string, object>[] slots,
        (string parameterName, int segmentIndex, int slotIndex)[] captures,
        in (string parameterName, int segmentIndex, int slotIndex) catchAll,
        (RoutePatternPathSegment pathSegment, int segmentIndex)[] complexSegments,
        KeyValuePair<string, IRouteConstraint>[] constraints)
    {
        Endpoint = endpoint;
        Score = score;
        Slots = slots;
        Captures = captures;
        CatchAll = catchAll;
        ComplexSegments = complexSegments;
        Constraints = constraints;

        Flags = CandidateFlags.None;
        for (var i = 0; i < slots.Length; i++)
        {
            if (slots[i].Key != null)
            {
                Flags |= CandidateFlags.HasDefaults;
            }
        }

        if (captures.Length > 0)
        {
            Flags |= CandidateFlags.HasCaptures;
        }

        if (catchAll.parameterName != null)
        {
            Flags |= CandidateFlags.HasCatchAll;
        }

        if (complexSegments.Length > 0)
        {
            Flags |= CandidateFlags.HasComplexSegments;
        }

        if (constraints.Length > 0)
        {
            Flags |= CandidateFlags.HasConstraints;
        }
    }

    [Flags]
    public enum CandidateFlags
    {
        None = 0,
        HasDefaults = 1,
        HasCaptures = 2,
        HasCatchAll = 4,
        HasSlots = HasDefaults | HasCaptures | HasCatchAll,
        HasComplexSegments = 8,
        HasConstraints = 16,
    }
}
