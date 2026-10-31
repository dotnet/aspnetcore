// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Mvc.ViewComponents;

/// <summary>
/// A <see cref="IViewComponentActivator"/> that retrieves view components as services from the request's
/// <see cref="IServiceProvider"/>.
/// </summary>
public class ServiceBasedViewComponentActivator : IViewComponentActivator
{
    /// <inheritdoc />
    public object Create(ViewComponentContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var viewComponentType = context.ViewComponentDescriptor.TypeInfo.AsType();

        return context.ViewContext.HttpContext.RequestServices.GetRequiredService(viewComponentType);
    }

    /// <inheritdoc />
    public virtual void Release(ViewComponentContext context, object viewComponent)
    {
    }
}
