// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.Framework.Internal;

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
        /// <param name="isPartial">Determines if the view being discovered is a partial.</param>
        public ViewLocationExpanderContext([NotNull] ActionContext actionContext,
                                           [NotNull] string viewName,
                                           bool isPartial)
        {
            ActionContext = actionContext;
            ViewName = viewName;
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