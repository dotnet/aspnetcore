// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Mvc.ViewComponents
{
    /// <summary>
    /// A <see cref="IViewComponentActivator"/> that retrieves view components as services from the request's
    /// <see cref="IServiceProvider"/>.
    /// </summary>
    public class ServiceBasedViewComponentActivator : IViewComponentActivator
    {
        /// <inheritdoc />
        public object Create(ViewComponentContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            var viewComponentType = context.ViewComponentDescriptor.TypeInfo.AsType();

            return context.ViewContext.HttpContext.RequestServices.GetRequiredService(viewComponentType);
        }

        /// <inheritdoc />
        public virtual void Release(ViewComponentContext context, object viewComponent)
        {
        }
    }
}
