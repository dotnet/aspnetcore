// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Abstractions;

namespace Microsoft.AspNetCore.Mvc.ActionConstraints;

/// <summary>
/// Context for an action constraint provider.
/// </summary>
public class ActionConstraintProviderContext
{
    /// <summary>
    /// Creates a new <see cref="ActionConstraintProviderContext"/>.
    /// </summary>
    /// <param name="context">The <see cref="Http.HttpContext"/> associated with the request.</param>
    /// <param name="action">The <see cref="ActionDescriptor"/> for which constraints are being created.</param>
    /// <param name="items">The list of <see cref="ActionConstraintItem"/> objects.</param>
    public ActionConstraintProviderContext(
        HttpContext context,
        ActionDescriptor action,
        IList<ActionConstraintItem> items)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(action);
        ArgumentNullException.ThrowIfNull(items);

        HttpContext = context;
        Action = action;
        Results = items;
    }

    /// <summary>
    /// The <see cref="Http.HttpContext"/> associated with the request.
    /// </summary>
    public HttpContext HttpContext { get; }

    /// <summary>
    /// The <see cref="ActionDescriptor"/> for which constraints are being created.
    /// </summary>
    public ActionDescriptor Action { get; }

    /// <summary>
    /// The list of <see cref="ActionConstraintItem"/> objects.
    /// </summary>
    public IList<ActionConstraintItem> Results { get; }
}
