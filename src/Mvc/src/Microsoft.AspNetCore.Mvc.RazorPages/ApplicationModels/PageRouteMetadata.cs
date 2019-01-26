// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Mvc.ApplicationModels
{
    // This is used to store the uncombined parts of the final page route
    // Note: This type name is referenced by name in AuthorizationMiddleware, do not change this without addressing https://github.com/aspnet/AspNetCore/issues/7011
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
