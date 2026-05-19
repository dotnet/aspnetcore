// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

namespace Microsoft.AspNetCore.Routing;

/// <summary>
/// A feature interface for routing functionality.
/// </summary>
public interface IRoutingFeature
{
    /// <summary>
    /// Gets or sets the <see cref="Routing.RouteData"/> associated with the current request.
    /// </summary>
    RouteData? RouteData { get; set; }
}
