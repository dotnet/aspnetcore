// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;

namespace Microsoft.AspNetCore.Mvc.Filters
{
    /// <summary>
    /// An abstract context for filters.
    /// </summary>
    public abstract class FilterContext : ActionContext
    {
        /// <summary>
        /// Instantiates a new <see cref="FilterContext"/> instance.
        /// </summary>
        /// <param name="actionContext">The <see cref="ActionContext"/>.</param>
        /// <param name="filters">All applicable <see cref="IFilterMetadata"/> implementations.</param>
        public FilterContext(
            ActionContext actionContext,
            IList<IFilterMetadata> filters)
            : base(actionContext)
        {
            if (filters == null)
            {
                throw new ArgumentNullException(nameof(filters));
            }

            Filters = filters;
        }

        /// <summary>
        /// Gets all applicable <see cref="IFilterMetadata"/> implementations.
        /// </summary>
        public virtual IList<IFilterMetadata> Filters { get; }
    }
}
