// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.Framework.Internal;

namespace Microsoft.AspNet.Mvc.Filters
{
    public abstract class FilterContext : ActionContext
    {
        public FilterContext(
            [NotNull] ActionContext actionContext,
            [NotNull] IList<IFilterMetadata> filters)
            : base(actionContext)
        {
            Filters = filters;
        }

        public virtual IList<IFilterMetadata> Filters { get; private set; }
    }
}
