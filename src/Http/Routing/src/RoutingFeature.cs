// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Routing;

/// <summary>
/// A feature for routing functionality.
/// </summary>
public class RoutingFeature : IRoutingFeature
{
    /// <inheritdoc />
    public RouteData? RouteData { get; set; }
}
