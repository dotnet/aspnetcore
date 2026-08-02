// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Mvc.Abstractions;

namespace Microsoft.AspNetCore.Mvc.ActionConstraints;

/// <summary>
/// A candidate action for action selection.
/// </summary>
public readonly struct ActionSelectorCandidate
{
    /// <summary>
    /// Creates a new <see cref="ActionSelectorCandidate"/>.
    /// </summary>
    /// <param name="action">The <see cref="ActionDescriptor"/> representing a candidate for selection.</param>
    /// <param name="constraints">
    /// The list of <see cref="IActionConstraint"/> instances associated with <paramref name="action"/>.
    /// </param>
    public ActionSelectorCandidate(ActionDescriptor action, IReadOnlyList<IActionConstraint>? constraints)
    {
        ArgumentNullException.ThrowIfNull(action);

        Action = action;
        Constraints = constraints;
    }

    /// <summary>
    /// The <see cref="ActionDescriptor"/> representing a candidate for selection.
    /// </summary>
    public ActionDescriptor Action { get; }

    /// <summary>
    /// The list of <see cref="IActionConstraint"/> instances associated with <see name="Action"/>.
    /// </summary>
    public IReadOnlyList<IActionConstraint>? Constraints { get; }
}
