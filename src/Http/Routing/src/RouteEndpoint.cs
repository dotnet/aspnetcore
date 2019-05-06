// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing.Patterns;

namespace Microsoft.AspNetCore.Routing
{
    /// <summary>
    /// Represents an <see cref="Endpoint"/> that can be used in URL matching or URL generation.
    /// </summary>
    public sealed class RouteEndpoint : Endpoint
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="RouteEndpoint"/> class.
        /// </summary>
        /// <param name="requestDelegate">The delegate used to process requests for the endpoint.</param>
        /// <param name="routePattern">The <see cref="RoutePattern"/> to use in URL matching.</param>
        /// <param name="order">The order assigned to the endpoint.</param>
        /// <param name="metadata">
        /// The <see cref="EndpointMetadataCollection"/> or metadata associated with the endpoint.
        /// </param>
        /// <param name="displayName">The informational display name of the endpoint.</param>
        public RouteEndpoint(
            RequestDelegate requestDelegate,
            RoutePattern routePattern,
            int order,
            EndpointMetadataCollection metadata,
            string displayName)
            : base(requestDelegate, metadata, displayName)
        {
            if (requestDelegate == null)
            {
                throw new ArgumentNullException(nameof(requestDelegate));
            }

            if (routePattern == null)
            {
                throw new ArgumentNullException(nameof(routePattern));
            }

            RoutePattern = routePattern;
            Order = order;
        }

        /// <summary>
        /// Gets the order value of endpoint.
        /// </summary>
        /// <remarks>
        /// The order value provides absolute control over the priority
        /// of an endpoint. Endpoints with a lower numeric value of order have higher priority.
        /// </remarks>
        public int Order { get; }
        
        /// <summary>
        /// Gets the <see cref="RoutePattern"/> associated with the endpoint.
        /// </summary>
        public RoutePattern RoutePattern { get; }
    }
}
