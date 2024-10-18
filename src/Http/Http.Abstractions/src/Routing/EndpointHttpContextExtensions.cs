// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http.Features;

namespace Microsoft.AspNetCore.Http;

/// <summary>
/// Extension methods to expose Endpoint on HttpContext.
/// </summary>
public static class EndpointHttpContextExtensions
{
    /// <summary>
    /// Extension method for getting the <see cref="Endpoint"/> for the current request.
    /// </summary>
    /// <param name="context">The <see cref="HttpContext"/> context.</param>
    /// <returns>The <see cref="Endpoint"/> or <c>null</c> if the request doesn't have an endpoint.</returns>
    /// <remarks>
    /// The endpoint for a request is typically set by routing middleware. A request might not have
    /// an endpoint if routing middleware hasn't run yet, or the request didn't match a route.
    /// </remarks>
    public static Endpoint? GetEndpoint(this HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        return context.Endpoint;
    }

    /// <summary>
    /// Extension method for setting the <see cref="Endpoint"/> for the current request.
    /// </summary>
    /// <param name="context">The <see cref="HttpContext"/> context.</param>
    /// <param name="endpoint">The <see cref="Endpoint"/>. A <c>null</c> value clears the endpoint for the current request.</param>
    public static void SetEndpoint(this HttpContext context, Endpoint? endpoint)
    {
        ArgumentNullException.ThrowIfNull(context);
        context.Endpoint = endpoint;
    }

    private sealed class EndpointFeature : IEndpointFeature
    {
        /// <summary>
        /// Gets or sets the <see cref="Endpoint"/> for the current request.
        /// </summary>
        public Endpoint? Endpoint { get; set; }
    }
}
