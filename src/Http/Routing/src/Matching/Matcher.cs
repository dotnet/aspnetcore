// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Routing.Matching;

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
    /// <returns>A <see cref="Task"/> which represents the asynchronous completion of the operation.</returns>
    public abstract Task MatchAsync(HttpContext httpContext);
}
