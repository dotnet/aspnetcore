// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Routing.Matchers
{
    internal class RouteConstraintMatchProcessor : MatchProcessor
    {
        public RouteConstraintMatchProcessor(string parameterName, IRouteConstraint constraint)
        {
            ParameterName = parameterName;
            Constraint = constraint;
        }

        public string ParameterName { get; }

        public IRouteConstraint Constraint { get; }

        public override bool ProcessInbound(HttpContext httpContext, RouteValueDictionary values)
        {
            if (httpContext == null)
            {
                throw new ArgumentNullException(nameof(httpContext));
            }

            if (values == null)
            {
                throw new ArgumentNullException(nameof(values));
            }

            return Constraint.Match(
                httpContext,
                NullRouter.Instance,
                ParameterName,
                values,
                RouteDirection.IncomingRequest);
        }

        public override bool ProcessOutbound(HttpContext httpContext, RouteValueDictionary values)
        {
            if (httpContext == null)
            {
                throw new ArgumentNullException(nameof(httpContext));
            }

            if (values == null)
            {
                throw new ArgumentNullException(nameof(values));
            }

            return Constraint.Match(
                httpContext,
                NullRouter.Instance,
                ParameterName,
                values,
                RouteDirection.UrlGeneration);
        }
    }
}
