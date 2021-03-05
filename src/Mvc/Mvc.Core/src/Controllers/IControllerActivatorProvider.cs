// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.Mvc.Controllers
{
    /// <summary>
    /// Provides methods to create a MVC controller.
    /// </summary>
    public interface IControllerActivatorProvider
    {
        /// <summary>
        /// Creates a <see cref="Func{T, TResult}"/> that creates a controller.
        /// </summary>
        /// <param name="descriptor">The <see cref="ControllerActionDescriptor"/>.</param>
        /// <returns>The delegate used to activate the controller.</returns>
        Func<ControllerContext, object> CreateActivator(ControllerActionDescriptor descriptor);

        /// <summary>
        /// Creates an <see cref="Action"/> that releases a controller.
        /// </summary>
        /// <param name="descriptor">The <see cref="ControllerActionDescriptor"/>.</param>
        /// <returns>The delegate used to dispose the activated controller.</returns>
        Action<ControllerContext, object> CreateReleaser(ControllerActionDescriptor descriptor);

        /// <summary>
        /// Creates an <see cref="Action"/> that releases a controller.
        /// </summary>
        /// <param name="descriptor">The <see cref="ControllerActionDescriptor"/>.</param>
        /// <returns>The delegate used to dispose the activated controller.</returns>
        Func<ControllerContext, object, ValueTask> CreateAsyncReleaser(ControllerActionDescriptor descriptor)
        {
            var releaser = CreateReleaser(descriptor);
            return (context, controller) =>
            {
                releaser(context, controller);
                return default;
            };
        }
    }
}
