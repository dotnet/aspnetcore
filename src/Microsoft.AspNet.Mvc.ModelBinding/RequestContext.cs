// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.AspNet.Http;

namespace Microsoft.AspNet.Mvc
{
    public class RequestContext
    {
        public RequestContext([NotNull]HttpContext context,
                              [NotNull]IDictionary<string, object> routeValues)
        {
            HttpContext = context;
            RouteValues = routeValues;
        }

        public virtual IDictionary<string, object> RouteValues { get; set; }

        public virtual HttpContext HttpContext { get; set; }
    }
}
