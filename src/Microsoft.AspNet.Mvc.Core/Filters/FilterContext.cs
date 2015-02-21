// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.Framework.Internal;

namespace Microsoft.AspNet.Mvc
{
    public abstract class FilterContext : ActionContext
    {
        public FilterContext(
            [NotNull] ActionContext actionContext,
            [NotNull] IList<IFilter> filters)
            : base(actionContext)
        {
            Filters = filters;
        }

        public virtual IList<IFilter> Filters { get; private set; }
    }
}
