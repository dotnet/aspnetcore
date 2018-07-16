// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Routing
{
    public abstract class LinkGenerator
    {
        public abstract bool TryGetLink(
            HttpContext httpContext,
            IEnumerable<Endpoint> endpoints,
            RouteValueDictionary explicitValues,
            RouteValueDictionary ambientValues,
            out string link);

        public abstract string GetLink(
            HttpContext httpContext,
            IEnumerable<Endpoint> endpoints,
            RouteValueDictionary explicitValues,
            RouteValueDictionary ambientValues);
    }
}
