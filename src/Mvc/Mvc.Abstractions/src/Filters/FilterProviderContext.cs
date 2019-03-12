// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;

namespace Microsoft.AspNetCore.Mvc.Filters
{
    /// <summary>
    /// A context for filter providers i.e. <see cref="IFilterProvider"/> implementations.
    /// </summary>
    public class FilterProviderContext
    {
        /// <summary>
        /// Instantiates a new <see cref="FilterProviderContext"/> instance.
        /// </summary>
        /// <param name="actionContext">The <see cref="ActionContext"/>.</param>
        /// <param name="items">
        /// The <see cref="FilterItem"/>s, initially created from <see cref="FilterDescriptor"/>s or a cache entry.
        /// </param>
        public FilterProviderContext(ActionContext actionContext, IList<FilterItem> items)
        {
            if (actionContext == null)
            {
                throw new ArgumentNullException(nameof(actionContext));
            }

            if (items == null)
            {
                throw new ArgumentNullException(nameof(items));
            }

            ActionContext = actionContext;
            Results = items;
        }

        /// <summary>
        /// Gets or sets the <see cref="ActionContext"/>.
        /// </summary>
        public ActionContext ActionContext { get; set; }

        /// <summary>
        /// Gets or sets the <see cref="FilterItem"/>s, initially created from <see cref="FilterDescriptor"/>s or a
        /// cache entry. <see cref="IFilterProvider"/>s should set <see cref="FilterItem.Filter"/> on existing items or
        /// add new <see cref="FilterItem"/>s to make executable filters available.
        /// </summary>
        public IList<FilterItem> Results { get; set; }
    }
}
