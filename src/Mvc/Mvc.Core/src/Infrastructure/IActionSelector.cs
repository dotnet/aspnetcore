// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Routing;

namespace Microsoft.AspNetCore.Mvc.Infrastructure;

/// <summary>
/// Defines an interface for selecting an MVC action to invoke for the current request.
/// </summary>
public interface IActionSelector
{
    /// <summary>
    /// Selects a set of <see cref="ActionDescriptor"/> candidates for the current request associated with
    /// <paramref name="context"/>.
    /// </summary>
    /// <param name="context">The <see cref="RouteContext"/> associated with the current request.</param>
    /// <returns>A set of <see cref="ActionDescriptor"/> candidates or <c>null</c>.</returns>
    /// <remarks>
    /// <para>
    /// Used by conventional routing to select the set of actions that match the route values for the
    /// current request. Action constraints associated with the candidates are not invoked by this method
    /// </para>
    /// <para>
    /// Attribute routing does not call this method.
    /// </para>
    /// </remarks>
    IReadOnlyList<ActionDescriptor>? SelectCandidates(RouteContext context);

    /// <summary>
    /// Selects the best <see cref="ActionDescriptor"/> candidate from <paramref name="candidates"/> for the
    /// current request associated with <paramref name="context"/>.
    /// </summary>
    /// <param name="context">The <see cref="RouteContext"/> associated with the current request.</param>
    /// <param name="candidates">The set of <see cref="ActionDescriptor"/> candidates.</param>
    /// <returns>The best <see cref="ActionDescriptor"/> candidate for the current request or <c>null</c>.</returns>
    /// <exception cref="AmbiguousActionException">
    /// Thrown when action selection results in an ambiguity.
    /// </exception>
    /// <remarks>
    /// <para>
    /// Invokes action constraints associated with the candidates.
    /// </para>
    /// <para>
    /// Used by conventional routing after calling <see cref="SelectCandidates"/> to apply action constraints and
    /// disambiguate between multiple candidates.
    /// </para>
    /// <para>
    /// Used by attribute routing to apply action constraints and disambiguate between multiple candidates.
    /// </para>
    /// </remarks>
    ActionDescriptor? SelectBestCandidate(RouteContext context, IReadOnlyList<ActionDescriptor> candidates);
}
