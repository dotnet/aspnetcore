// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;

namespace Microsoft.AspNetCore.Mvc.Filters
{
    /// <summary>
    /// A context for action filters, specifically <see cref="IActionFilter.OnActionExecuted"/> and
    /// <see cref="IAsyncActionFilter.OnActionExecutionAsync"/> calls.
    /// </summary>
    public class ActionExecutingContext : FilterContext
    {
        /// <summary>
        /// Instantiates a new <see cref="ActionExecutingContext"/> instance.
        /// </summary>
        /// <param name="actionContext">The <see cref="ActionContext"/>.</param>
        /// <param name="filters">All applicable <see cref="IFilterMetadata"/> implementations.</param>
        /// <param name="actionArguments">
        /// The arguments to pass when invoking the action. Keys are parameter names.
        /// </param>
        /// <param name="controller">The controller instance containing the action.</param>
        public ActionExecutingContext(
            ActionContext actionContext,
            IList<IFilterMetadata> filters,
            IDictionary<string, object> actionArguments,
            object controller)
            : base(actionContext, filters)
        {
            if (actionArguments == null)
            {
                throw new ArgumentNullException(nameof(actionArguments));
            }

            ActionArguments = actionArguments;
            Controller = controller;
        }

        /// <summary>
        /// Gets or sets the <see cref="IActionResult"/> to execute. Setting <see cref="Result"/> to a non-<c>null</c>
        /// value inside an action filter will short-circuit the action and any remaining action filters.
        /// </summary>
        public virtual IActionResult Result { get; set; }

        /// <summary>
        /// Gets the arguments to pass when invoking the action. Keys are parameter names.
        /// </summary>
        public virtual IDictionary<string, object> ActionArguments { get; }

        /// <summary>
        /// Gets the controller instance containing the action.
        /// </summary>
        public virtual object Controller { get; }
    }
}
