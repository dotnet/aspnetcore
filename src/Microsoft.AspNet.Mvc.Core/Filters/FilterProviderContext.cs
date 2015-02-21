// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.Framework.Internal;

namespace Microsoft.AspNet.Mvc
{
    public class FilterProviderContext
    {
        public FilterProviderContext([NotNull] ActionContext actionContext, [NotNull] IList<FilterItem> items)
        {
            ActionContext = actionContext;
            Results = items;
        }

        // Input
        public ActionContext ActionContext { get; set; }

        // Results
        public IList<FilterItem> Results { get; set; }
    }
}
