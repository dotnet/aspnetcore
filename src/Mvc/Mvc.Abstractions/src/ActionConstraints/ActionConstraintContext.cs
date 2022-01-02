// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Routing;

namespace Microsoft.AspNetCore.Mvc.ActionConstraints;

/// <summary>
/// Context for <see cref="IActionConstraint"/> execution.
/// </summary>
public class ActionConstraintContext
{
    /// <summary>
    /// The list of <see cref="ActionSelectorCandidate"/>. This includes all actions that are valid for the current
    /// request, as well as their constraints.
    /// </summary>
    public IReadOnlyList<ActionSelectorCandidate> Candidates { get; set; } = Array.Empty<ActionSelectorCandidate>();

    /// <summary>
    /// The current <see cref="ActionSelectorCandidate"/>.
    /// </summary>
    public ActionSelectorCandidate CurrentCandidate { get; set; } = default!;

    /// <summary>
    /// The <see cref="RouteContext"/>.
    /// </summary>
    public RouteContext RouteContext { get; set; } = default!;
}
