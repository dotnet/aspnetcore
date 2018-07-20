// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing.Patterns;

namespace Microsoft.AspNetCore.Routing.Matchers
{
    public sealed class MatcherEndpoint : Endpoint
    {
        internal static readonly Func<RequestDelegate, RequestDelegate> EmptyInvoker = (next) =>
        {
            return (context) => Task.CompletedTask;
        };

        public MatcherEndpoint(
            Func<RequestDelegate, RequestDelegate> invoker,
            RoutePattern routePattern,
            RouteValueDictionary requiredValues,
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
            RequiredValues = requiredValues;
            Order = order;
        }

        public Func<RequestDelegate, RequestDelegate> Invoker { get; }

        public int Order { get; }
        
        // Values required by an endpoint for it to be successfully matched on link generation
        public IReadOnlyDictionary<string, object> RequiredValues { get; }

        public RoutePattern RoutePattern { get; }
    }
}
