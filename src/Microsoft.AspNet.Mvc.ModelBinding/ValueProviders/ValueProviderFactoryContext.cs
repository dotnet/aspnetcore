// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.AspNet.Http;
using Microsoft.Framework.Internal;

namespace Microsoft.AspNet.Mvc.ModelBinding
{
    public class ValueProviderFactoryContext
    {
        public ValueProviderFactoryContext(
            [NotNull] HttpContext httpContext,
            [NotNull] IDictionary<string, object> routeValues)
        {
            HttpContext = httpContext;
            RouteValues = routeValues;
        }

        public HttpContext HttpContext { get; private set; }

        public IDictionary<string, object> RouteValues { get; private set; }
    }
}