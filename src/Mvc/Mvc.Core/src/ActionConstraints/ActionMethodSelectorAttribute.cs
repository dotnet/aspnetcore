// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Routing;

namespace Microsoft.AspNetCore.Mvc.ActionConstraints;

/// <summary>
/// Base class for attributes which can implement conditional logic to enable or disable an action
/// for a given request. See <see cref="IActionConstraint"/>.
/// </summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
public abstract class ActionMethodSelectorAttribute : Attribute, IActionConstraint
{
    /// <inheritdoc />
    public int Order { get; set; }

    /// <inheritdoc />
    public bool Accept(ActionConstraintContext context)
    {
        return IsValidForRequest(context.RouteContext, context.CurrentCandidate.Action);
    }

    /// <summary>
    /// Determines whether the action selection is valid for the specified route context.
    /// </summary>
    /// <param name="routeContext">The route context.</param>
    /// <param name="action">Information about the action.</param>
    /// <returns>
    /// <see langword="true"/> if the action  selection is valid for the specified context;
    /// otherwise, <see langword="false"/>.
    /// </returns>
    public abstract bool IsValidForRequest(RouteContext routeContext, ActionDescriptor action);
}
