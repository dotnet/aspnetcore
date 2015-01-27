// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNet.Mvc
{
    /// <summary>
    /// Provides methods for creation and disposal of controllers.
    /// </summary>
    public interface IControllerFactory
    {
        /// <summary>
        /// Creates a new controller for the specified <paramref name="actionContext"/>.
        /// </summary>
        /// <param name="actionContext"><see cref="ActionContext"/> for the action to execute.</param>
        /// <returns>The controller.</returns>
        object CreateController(ActionContext actionContext);

        /// <summary>
        /// Releases a controller instance.
        /// </summary>
        /// <param name="controller">The controller.</param>
        void ReleaseController(object controller);
    }
}
