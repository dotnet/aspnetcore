// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNet.Mvc.ViewComponents
{
    /// <summary>
    /// Provides methods to activate an instantiated ViewComponent
    /// </summary>
    public interface IViewComponentActivator
    {
        /// <summary>
        /// When implemented in a type, activates an instantiated ViewComponent.
        /// </summary>
        /// <param name="viewComponent">The ViewComponent to activate.</param>
        /// <param name="context">The <see cref="ViewContext"/> for the executing <see cref="ViewComponent"/>.</param>
        void Activate(object viewComponent, ViewContext context);
    }
}