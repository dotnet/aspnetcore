// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Endpoints;

namespace Microsoft.AspNetCore.Routing
{
    public struct EndpointSelectorContext
    {
        private HttpContext _httpContext;

        public EndpointSelectorContext(HttpContext httpContext)
        {
            _httpContext = httpContext;
        }

        /// <summary>
        /// Gets or sets the selected <see cref="Http.Endpoint"/> for the current
        /// request.
        /// </summary>
        public Endpoint Endpoint
        {
            get
            {
                return _httpContext.GetEndpoint();
            }
            set
            {
                if (value != null)
                {
                    _httpContext.SetEndpoint(value);
                }
            }
        }

        /// <summary>
        /// Gets or sets the <see cref="RouteValueDictionary"/> associated with the currrent
        /// request.
        /// </summary>
        public RouteValueDictionary RouteValues
        {
            get
            {
                return _httpContext.Request.RouteValues;
            }
            set
            {
                _httpContext.Request.RouteValues = value;
            }
        }
    }
}
