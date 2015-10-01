// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.AspNet.Http;

namespace Microsoft.AspNet.Mvc.ModelBinding
{
    public class ValueProviderFactoryContext
    {
        public ValueProviderFactoryContext(
            HttpContext httpContext,
            IDictionary<string, object> routeValues)
        {
            if (httpContext == null)
            {
                throw new ArgumentNullException(nameof(httpContext));
            }

            if (routeValues == null)
            {
                throw new ArgumentNullException(nameof(routeValues));
            }

            HttpContext = httpContext;
            RouteValues = routeValues;
        }

        public HttpContext HttpContext { get; private set; }

        public IDictionary<string, object> RouteValues { get; private set; }
    }
}