// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Mvc.ActionConstraints;

/// <summary>
/// Supports conditional logic to determine whether or not an associated action is valid to be selected
/// for the given request.
/// </summary>
/// <remarks>
/// Action constraints have the secondary effect of making an action with a constraint applied a better
/// match than one without.
///
/// Consider two actions, 'A' and 'B' with the same action and controller name. Action 'A' only allows the
/// HTTP POST method (via a constraint) and action 'B' has no constraints.
///
/// If an incoming request is a POST, then 'A' is considered the best match because it both matches and
/// has a constraint. If an incoming request uses any other verb, 'A' will not be valid for selection
/// due to it's constraint, so 'B' is the best match.
///
///
/// Action constraints are also grouped according to their order value. Any constraints with the same
/// group value are considered to be part of the same application policy, and will be executed in the
/// same stage.
///
/// Stages run in ascending order based on the value of <see cref="Order"/>. Given a set of actions which
/// are candidates for selection, the next stage to run is the lowest value of <see cref="Order"/> for any
/// constraint of any candidate which is greater than the order of the last stage.
///
/// Once the stage order is identified, each action has all of its constraints in that stage executed.
/// If any constraint does not match, then that action is not a candidate for selection. If any actions
/// with constraints in the current state are still candidates, then those are the 'best' actions and this
/// process will repeat with the next stage on the set of 'best' actions. If after processing the
/// subsequent stages of the 'best' actions no candidates remain, this process will repeat on the set of
/// 'other' candidate actions from this stage (those without a constraint).
/// </remarks>
public interface IActionConstraint : IActionConstraintMetadata
{
    /// <summary>
    /// The constraint order.
    /// </summary>
    /// <remarks>
    /// Constraints are grouped into stages by the value of <see cref="Order"/>. See remarks on
    /// <see cref="IActionConstraint"/>.
    /// </remarks>
    int Order { get; }

    /// <summary>
    /// Determines whether an action is a valid candidate for selection.
    /// </summary>
    /// <param name="context">The <see cref="ActionConstraintContext"/>.</param>
    /// <returns>True if the action is valid for selection, otherwise false.</returns>
    bool Accept(ActionConstraintContext context);
}
