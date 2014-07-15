// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNet.Mvc.Rendering
{
    /// <summary>
    /// Defines the contract for a view engine.
    /// </summary>
    public interface IViewEngine
    {
        /// <summary>
        /// Finds the specified view by using the specified action context.
        /// </summary>
        /// <param name="context">The action context.</param>
        /// <param name="viewName">The name or full path to the view.</param>
        /// <returns>A result representing the result of locating the view.</returns>
        ViewEngineResult FindView(ActionContext context, string viewName);

        /// <summary>
        /// Finds the specified partial view by using the specified action context.
        /// </summary>
        /// <param name="context">The action context.</param>
        /// <param name="viewName">The name or full path to the view.</param>
        /// <returns>A result representing the result of locating the view.</returns>
        ViewEngineResult FindPartialView(ActionContext context, string partialViewName);
    }
}
