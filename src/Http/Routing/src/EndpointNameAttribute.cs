// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Routing;

/// <summary>
/// Specifies the endpoint name in <see cref="Endpoint.Metadata"/>.
/// </summary>
/// <remarks>
/// Endpoint names must be unique within an application, and can be used to unambiguously
/// identify a desired endpoint for URI generation using <see cref="Microsoft.AspNetCore.Routing.LinkGenerator"/>
/// </remarks>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Delegate, Inherited = false, AllowMultiple = false)]
public sealed class EndpointNameAttribute : Attribute, IEndpointNameMetadata
{
    /// <summary>
    /// Initializes an instance of the EndpointNameAttribute.
    /// </summary>
    /// <param name="endpointName">The endpoint name.</param>
    public EndpointNameAttribute(string endpointName)
    {
        ArgumentNullException.ThrowIfNull(endpointName);

        EndpointName = endpointName;
    }

    /// <inheritdoc />
    public string EndpointName { get; }
}
