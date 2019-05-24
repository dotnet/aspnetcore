// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Mvc.Controllers
{
    /// <summary>
    /// Provides methods to create a controller.
    /// </summary>
    public interface IControllerActivator
    {
        /// <summary>
        /// Creates a controller.
        /// </summary>
        /// <param name="context">The <see cref="ControllerContext"/> for the executing action.</param>
        object Create(ControllerContext context);

        /// <summary>
        /// Releases a controller.
        /// </summary>
        /// <param name="context">The <see cref="ControllerContext"/> for the executing action.</param>
        /// <param name="controller">The controller to release.</param>
        void Release(ControllerContext context, object controller);
    }
}