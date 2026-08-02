// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Routing.Matching;

/// <summary>
/// The state associated with a candidate in a <see cref="CandidateSet"/>.
/// </summary>
public struct CandidateState
{
    internal CandidateState(Endpoint endpoint, int score)
    {
        Endpoint = endpoint;
        Score = score;
        Values = null;
    }

    internal CandidateState(Endpoint endpoint, RouteValueDictionary? values, int score)
    {
        Endpoint = endpoint;
        Values = values;
        Score = score;
    }

    /// <summary>
    /// Gets the <see cref="Http.Endpoint"/>.
    /// </summary>
    public Endpoint Endpoint { get; }

    /// <summary>
    /// Gets the score of the <see cref="Http.Endpoint"/> within the current
    /// <see cref="CandidateSet"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Candidates within a set are ordered in priority order and then assigned a
    /// sequential score value based on that ordering. Candidates with the same
    /// score are considered to have equal priority.
    /// </para>
    /// <para>
    /// The score values are used in the <see cref="EndpointSelector"/> to determine
    /// whether a set of matching candidates is an ambiguous match.
    /// </para>
    /// </remarks>
    public int Score { get; }

    /// <summary>
    /// Gets <see cref="RouteValueDictionary"/> associated with the
    /// <see cref="Http.Endpoint"/> and the current request.
    /// </summary>
    public RouteValueDictionary? Values { get; internal set; }
}
