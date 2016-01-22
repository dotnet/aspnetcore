// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNet.Mvc.Controllers
{
    /// <summary>
    /// Provides methods to create a controller.
    /// </summary>
    public interface IControllerActivator
    {
        /// <summary>
        /// Creates a controller.
        /// </summary>
        /// <param name="context">The <see cref="ActionContext"/> for the executing action.</param>
        object Create(ActionContext context, Type controllerType);
    }
}