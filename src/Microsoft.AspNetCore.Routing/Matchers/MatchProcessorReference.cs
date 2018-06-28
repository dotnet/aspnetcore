// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Routing.Matchers
{
    public sealed class MatchProcessorReference
    {
        // Example:
        // api/products/{productId:regex(\d+)}
        //
        // ParameterName = productId
        // ConstraintText = regex(\d+)
        // ConstraintArgument = \d+

        public MatchProcessorReference(string parameterName, string constraintText)
        {
            ParameterName = parameterName;
            ConstraintText = constraintText;
        }

        public MatchProcessorReference(string parameterName, bool optional, string constraintText)
        {
            ParameterName = parameterName;
            Optional = optional;
            ConstraintText = constraintText;
        }

        public MatchProcessorReference(string parameterName, MatchProcessor matchProcessor)
        {
            ParameterName = parameterName;
            MatchProcessor = matchProcessor;
        }

        internal MatchProcessor MatchProcessor { get; private set; }

        internal string ConstraintText { get; private set; }

        internal string ParameterName { get; private set; }

        internal bool Optional { get; private set; }

        public MatchProcessorReference(string parameterName, IRouteConstraint routeConstraint)
            : this(parameterName, new RouteConstraintMatchProcessorAdapter(parameterName, routeConstraint))
        {
        }

        private class RouteConstraintMatchProcessorAdapter : MatchProcessor
        {
            public string ParameterName { get; private set; }

            public IRouteConstraint RouteConstraint { get; }

            public RouteConstraintMatchProcessorAdapter(string parameterName, IRouteConstraint routeConstraint)
            {
                ParameterName = parameterName;
                RouteConstraint = routeConstraint;
            }

            public override void Initialize(string parameterName, string constraintArgument)
            {
            }

            public override bool ProcessInbound(HttpContext httpContext, RouteValueDictionary routeValues)
            {
                return RouteConstraint.Match(
                    httpContext,
                    NullRouter.Instance,
                    ParameterName,
                    routeValues,
                    RouteDirection.IncomingRequest);
            }

            public override bool ProcessOutbound(HttpContext httpContext, RouteValueDictionary values)
            {
                return RouteConstraint.Match(
                    httpContext,
                    NullRouter.Instance,
                    ParameterName,
                    values,
                    RouteDirection.UrlGeneration);
            }
        }
    }
}
