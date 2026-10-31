// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Routing;

/// <summary>
/// Defines a contract use to specify an endpoint name in <see cref="Endpoint.Metadata"/>.
/// </summary>
/// <remarks>
/// Endpoint names must be unique within an application, and can be used to unambiguously
/// identify a desired endpoint for URI generation using <see cref="LinkGenerator"/>.
/// </remarks>
public interface IEndpointNameMetadata
{
    /// <summary>
    /// Gets the endpoint name.
    /// </summary>
    string EndpointName { get; }
}
