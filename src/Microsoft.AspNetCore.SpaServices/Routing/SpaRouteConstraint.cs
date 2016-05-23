using System;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Microsoft.AspNetCore.SpaServices
{
    internal class SpaRouteConstraint : IRouteConstraint
    {
        private readonly string _clientRouteTokenName;

        public SpaRouteConstraint(string clientRouteTokenName)
        {
            if (string.IsNullOrEmpty(clientRouteTokenName))
            {
                throw new ArgumentException("Value cannot be null or empty", nameof(clientRouteTokenName));
            }

            _clientRouteTokenName = clientRouteTokenName;
        }

        public bool Match(
            HttpContext httpContext,
            IRouter route,
            string routeKey,
            RouteValueDictionary values,
            RouteDirection routeDirection)
            => !HasDotInLastSegment(values[_clientRouteTokenName] as string ?? string.Empty);

        private bool HasDotInLastSegment(string uri)
            => uri.IndexOf('.', uri.LastIndexOf('/') + 1) >= 0;
    }
}