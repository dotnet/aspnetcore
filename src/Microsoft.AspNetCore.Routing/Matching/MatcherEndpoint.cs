// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing.Patterns;

namespace Microsoft.AspNetCore.Routing.Matching
{
    /// <summary>
    /// Represents an <see cref="Endpoint"/> that can be used in URL matching or URL generation.
    /// </summary>
    public sealed class MatcherEndpoint : Endpoint
    {
        internal static readonly Func<RequestDelegate, RequestDelegate> EmptyInvoker = (next) =>
        {
            return (context) => Task.CompletedTask;
        };

        /// <summary>
        /// Initializes a new instance of the <see cref="MatcherEndpoint"/> class.
        /// </summary>
        /// <param name="invoker">The delegate to invoke to create a <see cref="RequestDelegate"/>.</param>
        /// <param name="routePattern">The <see cref="RoutePattern"/> to use in URL matching.</param>
        /// <param name="order">The order assigned to the endpoint.</param>
        /// <param name="metadata">
        /// The <see cref="EndpointMetadataCollection"/> or metadata associated with the endpoint.
        /// </param>
        /// <param name="displayName">The informational display name of the endpoint.</param>
        public MatcherEndpoint(
            Func<RequestDelegate, RequestDelegate> invoker,
            RoutePattern routePattern,
            int order,
            EndpointMetadataCollection metadata,
            string displayName)
            : base(metadata, displayName)
        {
            if (invoker == null)
            {
                throw new ArgumentNullException(nameof(invoker));
            }

            if (routePattern == null)
            {
                throw new ArgumentNullException(nameof(routePattern));
            }

            Invoker = invoker;
            RoutePattern = routePattern;
            Order = order;
        }

        /// <summary>
        /// Gets the invoker. The invoker is a delegate used to create a <see cref="RequestDelegate"/>.
        /// </summary>
        public Func<RequestDelegate, RequestDelegate> Invoker { get; }

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
