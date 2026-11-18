// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Routing.Patterns;

namespace Microsoft.AspNetCore.Components.Endpoints;

internal sealed class BlazorWebEndpointMetadata(RoutePattern routePattern)
{
    public RoutePattern RoutePattern { get; } = routePattern;
}
