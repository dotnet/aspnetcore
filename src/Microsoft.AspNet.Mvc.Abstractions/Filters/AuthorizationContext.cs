// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;

namespace Microsoft.AspNet.Mvc.Filters
{
    public class AuthorizationContext : FilterContext
    {
        public AuthorizationContext(
            ActionContext actionContext,
            IList<IFilterMetadata> filters)
            : base(actionContext, filters)
        {
        }

        public virtual IActionResult Result { get; set; }
    }
}
