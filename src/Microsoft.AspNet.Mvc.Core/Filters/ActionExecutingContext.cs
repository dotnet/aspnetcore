// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.Framework.Internal;

namespace Microsoft.AspNet.Mvc
{
    public class ActionExecutingContext : FilterContext
    {
        public ActionExecutingContext(
            [NotNull] ActionContext actionContext,
            [NotNull] IList<IFilter> filters,
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
