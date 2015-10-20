// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;

namespace Microsoft.AspNet.Mvc.Razor
{
    /// <summary>
    /// A context for containing information for <see cref="IViewLocationExpander"/>.
    /// </summary>
    public class ViewLocationExpanderContext
    {
        /// <summary>
        /// Initializes a new instance of <see cref="ViewLocationExpanderContext"/>.
        /// </summary>
        /// <param name="actionContext">The <see cref="Mvc.ActionContext"/> for the current executing action.</param>
        /// <param name="viewName">The view name.</param>
        /// <param name="controllerName">The controller name.</param>
        /// <param name="areaName">The area name.</param>
        /// <param name="isPartial">Determines if the view being discovered is a partial.</param>
        public ViewLocationExpanderContext(
            ActionContext actionContext,
            string viewName,
            string controllerName,
            string areaName,
            bool isPartial)
        {
            if (actionContext == null)
            {
                throw new ArgumentNullException(nameof(actionContext));
            }

            if (viewName == null)
            {
                throw new ArgumentNullException(nameof(viewName));
            }

            ActionContext = actionContext;
            ViewName = viewName;
            ControllerName = controllerName;
            AreaName = areaName;
            IsPartial = isPartial;
        }

        /// <summary>
        /// Gets the <see cref="Mvc.ActionContext"/> for the current executing action.
        /// </summary>
        public ActionContext ActionContext { get; }

        /// <summary>
        /// Gets the view name.
        /// </summary>
        public string ViewName { get; }

        /// <summary>
        /// Gets the controller name.
        /// </summary>
        public string ControllerName { get; }

        /// <summary>
        /// Gets the area name.
        /// </summary>
        public string AreaName { get; }

        /// <summary>
        /// Gets a value that determines if a partial view is being discovered.
        /// </summary>
        public bool IsPartial { get; }

        /// <summary>
        /// Gets or sets the <see cref="IDictionary{TKey, TValue}"/> that is populated with values as part of
        /// <see cref="IViewLocationExpander.PopulateValues(ViewLocationExpanderContext)"/>.
        /// </summary>
        public IDictionary<string, string> Values { get; set; }
    }
}