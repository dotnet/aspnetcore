// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;

namespace Microsoft.AspNet.Mvc.Filters
{
    public abstract class FilterContext : ActionContext
    {
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

        public virtual IList<IFilterMetadata> Filters { get; }
    }
}
