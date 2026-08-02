// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components.Routing;
using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Components.Endpoints;

/// <summary>
/// Invokes a Razor component endpoint to render the given root component and populates the
/// <see cref="IRoutingStateProvider"/> with the given metadata (if any) to render a given
/// page.
/// </summary>
/// <remarks>
/// Razor component endpoints provide the root component via the <see cref="RootComponentMetadata"/>
/// metadata in the endpoint.
/// The page component is provided via the <see cref="ComponentTypeMetadata"/>.
/// </remarks>
public interface IRazorComponentEndpointInvoker
{
    /// <summary>
    /// Invokes the Razor component endpoint.
    /// </summary>
    /// <param name="context">The <see cref="HttpContext"/> for the current request.</param>
    /// <returns>A <see cref="Task"/> that completes when the endpoint has been invoked and the component
    /// has been rendered into the response.</returns>
    Task Render(HttpContext context);
}
