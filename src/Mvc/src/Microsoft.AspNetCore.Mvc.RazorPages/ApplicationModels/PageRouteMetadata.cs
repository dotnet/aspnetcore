// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Mvc.ApplicationModels
{
    // This is used to store the uncombined parts of the final page route
    internal class PageRouteMetadata
    {
        public PageRouteMetadata(string pageRoute, string routeTemplate)
        {
            PageRoute = pageRoute;
            RouteTemplate = routeTemplate;
        }

        public string PageRoute { get; }
        public string RouteTemplate { get; }
    }
}
