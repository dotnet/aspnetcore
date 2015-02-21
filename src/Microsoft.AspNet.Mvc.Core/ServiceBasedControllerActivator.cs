// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.Framework.DependencyInjection;
using Microsoft.Framework.Internal;

namespace Microsoft.AspNet.Mvc
{
    /// <summary>
    /// A <see cref="IControllerActivator"/> that retrieves controllers as services from the request's
    /// <see cref="IServiceProvider"/>.
    /// </summary>
    public class ServiceBasedControllerActivator : IControllerActivator
    {
        /// <inheritdoc />
        public object Create([NotNull] ActionContext actionContext, [NotNull] Type controllerType)
        {
            return actionContext.HttpContext.RequestServices.GetRequiredService(controllerType);
        }
    }
}
