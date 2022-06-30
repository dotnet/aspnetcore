// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Mvc;

namespace Microsoft.AspNetCore.Http;

/// <summary>
/// Represent the current problem detatils context for the request.
/// </summary>
public sealed class ProblemDetailsContext
{
    /// <summary>
    /// Creates a new instance of the <see cref="ProblemDetailsContext"/>.
    /// </summary>
    /// <param name="httpContext">The <see cref="HttpContext"/> associated with the current request being processed by the filter.</param>
    public ProblemDetailsContext(HttpContext httpContext)
    {
        ArgumentNullException.ThrowIfNull(httpContext);

        HttpContext = httpContext;
    }

    /// <summary>
    /// The <see cref="HttpContext"/> associated with the current request being processed by the filter.
    /// </summary>
    public HttpContext HttpContext { get; }

    /// <summary>
    /// A collection of additional arbitrary metadata associated with the current request endpoint.
    /// </summary>
    public EndpointMetadataCollection? AdditionalMetadata { get; init; }

    /// <summary>
    /// A instance of <see cref="ProblemDetails"/> that will be
    /// used during the response payload generation.
    /// </summary>
    public ProblemDetails ProblemDetails { get; init; } = new ProblemDetails();
}
