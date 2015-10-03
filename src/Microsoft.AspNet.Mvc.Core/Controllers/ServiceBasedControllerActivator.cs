// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNet.Mvc.Controllers
{
    /// <summary>
    /// A <see cref="IControllerActivator"/> that retrieves controllers as services from the request's
    /// <see cref="IServiceProvider"/>.
    /// </summary>
    public class ServiceBasedControllerActivator : IControllerActivator
    {
        /// <inheritdoc />
        public object Create(ActionContext actionContext, Type controllerType)
        {
            if (actionContext == null)
            {
                throw new ArgumentNullException(nameof(actionContext));
            }

            if (controllerType == null)
            {
                throw new ArgumentNullException(nameof(controllerType));
            }

            return actionContext.HttpContext.RequestServices.GetRequiredService(controllerType);
        }
    }
}
