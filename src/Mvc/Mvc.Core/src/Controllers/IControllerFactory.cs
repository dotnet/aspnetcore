// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading.Tasks;

namespace Microsoft.AspNetCore.Mvc.Controllers
{
    /// <summary>
    /// Provides methods for creation and disposal of controllers.
    /// </summary>
    public interface IControllerFactory
    {
        /// <summary>
        /// Creates a new controller for the specified <paramref name="context"/>.
        /// </summary>
        /// <param name="context"><see cref="ControllerContext"/> for the action to execute.</param>
        /// <returns>The controller.</returns>
        object CreateController(ControllerContext context);

        /// <summary>
        /// Releases a controller instance.
        /// </summary>
        /// <param name="context"><see cref="ControllerContext"/> for the executing action.</param>
        /// <param name="controller">The controller.</param>
        void ReleaseController(ControllerContext context, object controller);

        /// <summary>
        /// Releases a controller instance asynchronously.
        /// </summary>
        /// <param name="context"><see cref="ControllerContext"/> for the executing action.</param>
        /// <param name="controller">The controller.</param>
        ValueTask ReleaseControllerAsync(ControllerContext context, object controller)
        {
            ReleaseController(context, controller);
            return default;
        }
    }
}
