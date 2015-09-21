// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.Framework.Internal;

namespace Microsoft.AspNet.Mvc.Filters
{
    public class ActionExecutingContext : FilterContext
    {
        public ActionExecutingContext(
            [NotNull] ActionContext actionContext,
            [NotNull] IList<IFilterMetadata> filters,
            [NotNull] IDictionary<string, object> actionArguments,
            object controller)
            : base(actionContext, filters)
        {
            ActionArguments = actionArguments;
            Controller = controller;
        }

        public virtual IActionResult Result { get; set; }

        public virtual IDictionary<string, object> ActionArguments { get; }

        public virtual object Controller { get; }
    }
}
