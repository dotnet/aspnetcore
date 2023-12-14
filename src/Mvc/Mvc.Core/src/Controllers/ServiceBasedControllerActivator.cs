// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Mvc.Controllers;

/// <summary>
/// A <see cref="IControllerActivator"/> that retrieves controllers as services from the request's
/// <see cref="IServiceProvider"/>.
/// </summary>
public class ServiceBasedControllerActivator : IControllerActivator
{
    /// <inheritdoc />
    public object Create(ControllerContext actionContext)
    {
        ArgumentNullException.ThrowIfNull(actionContext);

        var controllerType = actionContext.ActionDescriptor.ControllerTypeInfo.AsType();

        return actionContext.HttpContext.RequestServices.GetRequiredService(controllerType);
    }

    /// <inheritdoc />
    public virtual void Release(ControllerContext context, object controller)
    {
    }
}
