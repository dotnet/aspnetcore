// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Routing;

namespace Microsoft.AspNetCore.Http.Features
{
    /// <summary>
    /// A feature for routing values. Use <see cref="HttpContext.Features"/>
    /// to access the values associated with the current request.
    /// </summary>
    public class RouteValuesFeature : IRouteValuesFeature
    {
        private RouteValueDictionary _routeValues;

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
}