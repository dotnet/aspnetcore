// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;

namespace Microsoft.AspNet.Mvc.Filters
{
    public class FilterProviderContext
    {
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

        // Input
        public ActionContext ActionContext { get; set; }

        // Results
        public IList<FilterItem> Results { get; set; }
    }
}
