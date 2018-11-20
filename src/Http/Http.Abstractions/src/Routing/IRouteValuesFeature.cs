// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Routing;

namespace Microsoft.AspNetCore.Http.Features
{
    /// <summary>
    /// A feature interface for routing values. Use <see cref="HttpContext.Features"/>
    /// to access the values associated with the current request.
    /// </summary>
    public interface IRouteValuesFeature
    {
        /// <summary>
        /// Gets or sets the <see cref="RouteValueDictionary"/> associated with the currrent
        /// request.
        /// </summary>
        RouteValueDictionary RouteValues { get; set; }
    }
}
