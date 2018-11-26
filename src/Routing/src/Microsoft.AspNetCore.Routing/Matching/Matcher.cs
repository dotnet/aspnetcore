// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;

namespace Microsoft.AspNetCore.Routing.Matching
{
    /// <summary>
    /// An interface for components that can select an <see cref="Endpoint"/> given the current request, as part
    /// of the execution of <see cref="EndpointRoutingMiddleware"/>.
    /// </summary>
    internal abstract class Matcher
    {
        /// <summary>
        /// Attempts to asynchronously select an <see cref="Endpoint"/> for the current request.
        /// </summary>
        /// <param name="httpContext">The <see cref="HttpContext"/> associated with the current request.</param>
        /// <param name="context">
        /// The <see cref="IEndpointFeature"/> associated with the current request. The 
        /// <see cref="EndpointSelectorContext"/> will be mutated to contain the result of the operation.</param>
        /// <returns>A <see cref="Task"/> which represents the asynchronous completion of the operation.</returns>
        public abstract Task MatchAsync(HttpContext httpContext, EndpointSelectorContext context);
    }
}
