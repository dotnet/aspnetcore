// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Routing
{
    /// <summary>
    /// Defines a contract for a handler of a route. 
    /// </summary>
    public interface IRouteHandler
    {
        /// <summary>
        /// Gets a <see cref="RequestDelegate"/> to handle the request, based on the provided
        /// <paramref name="routeData"/>.
        /// </summary>
        /// <param name="httpContext">The <see cref="HttpContext"/> associated with the current request.</param>
        /// <param name="routeData">The <see cref="RouteData"/> associated with the current routing match.</param>
        /// <returns>
        /// A <see cref="RequestDelegate"/>, or <c>null</c> if the handler cannot handle this request.
        /// </returns>
        RequestDelegate GetRequestHandler(HttpContext httpContext, RouteData routeData);
    }
}
