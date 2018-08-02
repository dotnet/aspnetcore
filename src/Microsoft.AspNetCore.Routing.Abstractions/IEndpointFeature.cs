// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Routing
{
    /// <summary>
    /// A feature interface for endpoint routing. Use <see cref="HttpContext.Features"/>
    /// to access an instance associated with the current request.
    /// </summary>
    public interface IEndpointFeature
    {
        /// <summary>
        /// Gets or sets the selected <see cref="Routing.Endpoint"/> for the current
        /// request.
        /// </summary>
        Endpoint Endpoint { get; set; }

        /// <summary>
        /// Gets or sets a delegate that can be used to invoke the current
        /// <see cref="Routing.Endpoint"/>.
        /// </summary>
        Func<RequestDelegate, RequestDelegate> Invoker { get; set; }

        /// <summary>
        /// Gets or sets the <see cref="RouteValueDictionary"/> associated with the currrent
        /// request.
        /// </summary>
        RouteValueDictionary Values { get; set; }
    }
}
