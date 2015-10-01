// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNet.Mvc.Infrastructure;

namespace Microsoft.AspNet.Mvc.Controllers
{
    /// <summary>
    /// <see cref="IControllerActivator"/> that uses type activation to create controllers.
    /// </summary>
    public class DefaultControllerActivator : IControllerActivator
    {
        private readonly ITypeActivatorCache _typeActivatorCache;

        /// <summary>
        /// Creates a new <see cref="DefaultControllerActivator"/>.
        /// </summary>
        /// <param name="typeActivatorCache">The <see cref="ITypeActivatorCache"/>.</param>
        public DefaultControllerActivator(ITypeActivatorCache typeActivatorCache)
        {
            _typeActivatorCache = typeActivatorCache;
        }
        /// <inheritdoc />
        public virtual object Create(ActionContext actionContext, Type controllerType)
        {
            if (actionContext == null)
            {
                throw new ArgumentNullException(nameof(actionContext));
            }

            if (controllerType == null)
            {
                throw new ArgumentNullException(nameof(controllerType));
            }

            var serviceProvider = actionContext.HttpContext.RequestServices;
            return _typeActivatorCache.CreateInstance<object>(serviceProvider, controllerType);
        }
    }
}
