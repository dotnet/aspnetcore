// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Builder;

namespace Microsoft.AspNetCore.Routing;

/// <summary>
/// Defines a contract for a route builder in an application. A route builder specifies the routes for
/// an application.
/// </summary>
public interface IEndpointRouteBuilder
{
    /// <summary>
    /// Creates a new <see cref="IApplicationBuilder"/>.
    /// </summary>
    /// <returns>The new <see cref="IApplicationBuilder"/>.</returns>
    IApplicationBuilder CreateApplicationBuilder();

    /// <summary>
    /// Gets the <see cref="IServiceProvider"/> used to resolve services for routes.
    /// </summary>
    IServiceProvider ServiceProvider { get; }

    /// <summary>
    /// Gets the endpoint data sources configured in the builder.
    /// </summary>
    ICollection<EndpointDataSource> DataSources { get; }
}
