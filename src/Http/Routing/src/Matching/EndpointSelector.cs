// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Routing.Matching;

/// <summary>
/// A service that is responsible for the final <see cref="Endpoint"/> selection
/// decision. To use a custom <see cref="EndpointSelector"/> register an implementation
/// of <see cref="EndpointSelector"/> in the dependency injection container as a singleton.
/// </summary>
public abstract class EndpointSelector
{
    /// <summary>
    /// Asynchronously selects an <see cref="Endpoint"/> from the <see cref="CandidateSet"/>.
    /// </summary>
    /// <param name="httpContext">The <see cref="HttpContext"/> associated with the current request.</param>
    /// <param name="candidates">The <see cref="CandidateSet"/>.</param>
    /// <returns>A <see cref="Task"/> that completes asynchronously once endpoint selection is complete.</returns>
    /// <remarks>
    /// An <see cref="EndpointSelector"/> should assign the endpoint by calling
    /// <see cref="EndpointHttpContextExtensions.SetEndpoint(HttpContext, Endpoint)"/>
    /// and setting <see cref="HttpRequest.RouteValues"/> once an endpoint is selected.
    /// </remarks>
    public abstract Task SelectAsync(HttpContext httpContext, CandidateSet candidates);
}
