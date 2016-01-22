// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;

namespace Microsoft.AspNet.Mvc.Filters
{
    public class ActionExecutingContext : FilterContext
    {
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

        public virtual IActionResult Result { get; set; }

        public virtual IDictionary<string, object> ActionArguments { get; }

        public virtual object Controller { get; }
    }
}
