// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Routing;

namespace Microsoft.AspNetCore.Http;

/// <summary>
/// Defines a contract that represents the result of an HTTP result endpoint
/// that constains an object route.
/// </summary>
public interface IAtRouteHttpResult : IResult
{
    /// <summary>
    /// Gets the name of the route to use for generating the URL.
    /// </summary>
    string? RouteName { get; }

    /// <summary>
    /// Gets the route data to use for generating the URL.
    /// </summary>
    RouteValueDictionary? RouteValues { get; }
}
