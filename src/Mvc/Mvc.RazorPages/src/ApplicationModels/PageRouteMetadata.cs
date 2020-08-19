// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Mvc.ApplicationModels
{
    /// <summary>
    /// Metadata used to construct an endpoint route to the page.
    /// </summary>
    // Note: This type name is referenced by name in AuthorizationMiddleware, do not change this without addressing https://github.com/dotnet/aspnetcore/issues/7011
    public sealed class PageRouteMetadata
    {
        /// <summary>
        /// Initializes a new instance of <see cref="PageRouteMetadata"/>.
        /// </summary>
        /// <param name="pageRoute">The page route.</param>
        /// <param name="routeTemplate">The route template specified by the page.</param>
        public PageRouteMetadata(string pageRoute, string routeTemplate)
        {
            PageRoute = pageRoute;
            RouteTemplate = routeTemplate;
        }

        /// <summary>
        /// Gets the page route.
        /// </summary>
        public string PageRoute { get; }

        /// <summary>
        /// Gets the route template specified by the page.
        /// </summary>
        public string RouteTemplate { get; }
    }
}
