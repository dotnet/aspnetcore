// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.AspNetCore.Routing.Patterns;

namespace Microsoft.AspNetCore.Components.Endpoints;

internal sealed class BlazorWebEndpointMetadata(RoutePattern routePattern) : IRouteDiagnosticsMetadata
{
    // Keep the framework pattern and diagnostics shared so independently configured metadata is never ignored.
    public RoutePattern RoutePattern { get; } = routePattern;

    public string Route => RoutePattern.RawText!;
}
