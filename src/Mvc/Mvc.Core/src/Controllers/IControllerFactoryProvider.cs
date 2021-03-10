// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.Mvc.Controllers
{
    /// <summary>
    /// Provides methods to create and release a controller.
    /// </summary>
    public interface IControllerFactoryProvider
    {
        /// <summary>
        /// Creates a factory for producing controllers for the specified <paramref name="descriptor"/>.
        /// </summary>
        /// <param name="descriptor">The <see cref="ControllerActionDescriptor"/>.</param>
        /// <returns>The controller factory.</returns>
        Func<ControllerContext, object> CreateControllerFactory(ControllerActionDescriptor descriptor);

        /// <summary>
        /// Releases a controller.
        /// </summary>
        /// <param name="descriptor">The <see cref="ControllerActionDescriptor"/>.</param>
        /// <returns>The delegate used to release the created controller.</returns>
        Action<ControllerContext, object> CreateControllerReleaser(ControllerActionDescriptor descriptor);

        /// <summary>
        /// Releases a controller asynchronously.
        /// </summary>
        /// <param name="descriptor">The <see cref="ControllerActionDescriptor"/>.</param>
        /// <returns>The delegate used to release the created controller asynchronously.</returns>
        Func<ControllerContext, object, ValueTask> CreateAsyncControllerReleaser(ControllerActionDescriptor descriptor)
        {
            var releaser = CreateControllerReleaser(descriptor);
            return (context, controller) =>
            {
                releaser(context, controller);
                return default;
            };
        }
    }
}
