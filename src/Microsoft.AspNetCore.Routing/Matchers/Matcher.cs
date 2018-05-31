// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Routing.Matchers
{
    /// <summary>
    /// An interface for components that can select an <see cref="Endpoint"/> given the current request, as part
    /// of the execution of <see cref="DispatcherMiddleware"/>.
    /// </summary>
    internal abstract class Matcher
    {
        /// <summary>
        /// Attempts to asynchronously select an <see cref="Endpoint"/> for the current request.
        /// </summary>
        /// <param name="httpContext">The <see cref="HttpContext"/> associated with the current request.</param>
        /// <param name="feature">
        /// The <see cref="IEndpointFeature"/> associated with the current request. The 
        /// <see cref="IEndpointFeature"/> will be mutated to contain the result of the operation.</param>
        /// <returns>A <see cref="Task"/> which represents the asynchronous completion of the operation.</returns>
        public abstract Task MatchAsync(HttpContext httpContext, IEndpointFeature feature);
    }
}
