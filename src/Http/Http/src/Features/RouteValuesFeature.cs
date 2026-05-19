// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Routing;

namespace Microsoft.AspNetCore.Http.Features;

/// <summary>
/// A feature for routing values. Use <see cref="HttpContext.Features"/>
/// to access the values associated with the current request.
/// </summary>
public class RouteValuesFeature : IRouteValuesFeature
{
    private RouteValueDictionary? _routeValues;

    /// <summary>
    /// Gets or sets the <see cref="RouteValueDictionary"/> associated with the currrent
    /// request.
    /// </summary>
    public RouteValueDictionary RouteValues
    {
        get
        {
            if (_routeValues == null)
            {
                _routeValues = new RouteValueDictionary();
            }

            return _routeValues;
        }
        set => _routeValues = value;
    }
}
