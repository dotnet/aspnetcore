// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNet.Mvc
{
    /// <summary>
    /// Provides methods to activate an instantiated controller.
    /// </summary>
    public interface IControllerActivator
    {
        /// <summary>
        /// When implemented in a type, activates an instantiated controller.
        /// </summary>
        /// <param name="controller">The controller to activate.</param>
        /// <param name="context">The <see cref="ActionContext"/> for the executing action.</param>
        void Activate(object controller, ActionContext context);
    }
}