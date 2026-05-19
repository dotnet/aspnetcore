// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Mvc.Abstractions;

/// <summary>
/// A context for <see cref="IActionInvokerProvider"/>.
/// </summary>
public class ActionInvokerProviderContext
{
    /// <summary>
    /// Initializes a new instance of <see cref="ActionInvokerProviderContext"/>.
    /// </summary>
    /// <param name="actionContext">The <see cref="Mvc.ActionContext"/> to invoke.</param>
    public ActionInvokerProviderContext(ActionContext actionContext)
    {
        ArgumentNullException.ThrowIfNull(actionContext);

        ActionContext = actionContext;
    }

    /// <summary>
    /// Gets the <see cref="Mvc.ActionContext"/> to invoke.
    /// </summary>
    public ActionContext ActionContext { get; }

    /// <summary>
    /// Gets or sets the <see cref="IActionInvoker"/> that will be used to invoke <see cref="ActionContext" />
    /// </summary>
    public IActionInvoker? Result { get; set; }
}
