// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components.Routing;

namespace Microsoft.AspNetCore.Components.Endpoints.DependencyInjection;

internal sealed class EndpointRoutingStateProvider : IRoutingStateProvider
{
    public RouteData? RouteData { get; internal set; }
}
