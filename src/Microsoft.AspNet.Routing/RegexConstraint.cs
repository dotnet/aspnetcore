using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;
using Microsoft.AspNet.Abstractions;

namespace Microsoft.AspNet.Routing
{
    public class RegexConstraint : IRouteConstraint
    {
        public RegexConstraint([NotNull] Regex regex)
        {
            Constraint = regex;
        }

        public RegexConstraint([NotNull] string regexPattern)
        {
            Constraint = new Regex(regexPattern, RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);
        }

        public Regex Constraint { get; private set; }

        public bool Match([NotNull] HttpContext httpContext,
                          [NotNull] IRouter route,
                          [NotNull] string routeKey,
                          [NotNull] IDictionary<string, object> routeValues,
                          RouteDirection routeDirection)
        {
            object routeValue;

            if (routeValues.TryGetValue(routeKey, out routeValue)
                && routeValue != null)
            {
                var parameterValueString = Convert.ToString(routeValue, CultureInfo.InvariantCulture);

                return Constraint.IsMatch(parameterValueString);
            }

            return false;
        }
    }
}
