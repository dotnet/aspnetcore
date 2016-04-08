using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Microsoft.AspNetCore.SpaServices
{
    internal class SpaRouteConstraint : IRouteConstraint
    {
        private readonly string clientRouteTokenName;

        public SpaRouteConstraint(string clientRouteTokenName) {
            if (string.IsNullOrEmpty(clientRouteTokenName)) {
                throw new ArgumentException("Value cannot be null or empty", "clientRouteTokenName");
            }

            this.clientRouteTokenName = clientRouteTokenName;
        }

        public bool Match(HttpContext httpContext, IRouter route, string routeKey, RouteValueDictionary values, RouteDirection routeDirection)
        {
            var clientRouteValue = (values[this.clientRouteTokenName] as string) ?? string.Empty;
            return !HasDotInLastSegment(clientRouteValue);
        }

        private bool HasDotInLastSegment(string uri)
        {
            var lastSegmentStartPos = uri.LastIndexOf('/');
            return uri.IndexOf('.', lastSegmentStartPos + 1) >= 0;
        }
    }
}
