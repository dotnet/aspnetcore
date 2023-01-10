// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Routing;

/// <summary>
/// Specifies an endpoint name in <see cref="Endpoint.Metadata"/>.
/// </summary>
/// <remarks>
/// Endpoint names must be unique within an application, and can be used to unambiguously
/// identify a desired endpoint for URI generation using <see cref="LinkGenerator"/>.
/// </remarks>
public class EndpointNameMetadata : IEndpointNameMetadata
{
    /// <summary>
    /// Creates a new instance of <see cref="EndpointNameMetadata"/> with the provided endpoint name.
    /// </summary>
    /// <param name="endpointName">The endpoint name.</param>
    public EndpointNameMetadata(string endpointName)
    {
        ArgumentNullException.ThrowIfNull(endpointName);

        EndpointName = endpointName;
    }

    /// <summary>
    /// Gets the endpoint name.
    /// </summary>
    public string EndpointName { get; }
}
