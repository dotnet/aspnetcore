// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;

namespace Microsoft.AspNet.Mvc.Razor
{
    /// <summary>
    /// A context for containing information for <see cref="IViewLocationExpander"/>.
    /// </summary>
    public class ViewLocationExpanderContext
    {
        public ViewLocationExpanderContext([NotNull] ActionContext actionContext,
                                           [NotNull] string viewName)
        {
            ActionContext = actionContext;
            ViewName = viewName;
        }

        /// <summary>
        /// Gets the <see cref="ActionContext"/> for the current executing action.
        /// </summary>
        public ActionContext ActionContext { get; private set; }

        /// <summary>
        /// Gets the view name 
        /// </summary>
        public string ViewName { get; private set; }

        /// <summary>
        /// Gets or sets the <see cref="IDictionary{TKey, TValue}"/> that is populated with values as part of
        /// <see cref="IViewLocationExpander.PopulateValues(ViewLocationExpanderContext)"/>.
        /// </summary>
        public IDictionary<string, string> Values { get; set; }
    }
}