// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Globalization;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Routing.Patterns;

namespace Microsoft.AspNetCore.Builder
{
    internal class MvcEndpointInfo
    {
        public MvcEndpointInfo(
            string name,
            string pattern,
            RouteValueDictionary defaults,
            IDictionary<string, object> constraints,
            RouteValueDictionary dataTokens,
            ParameterPolicyFactory parameterPolicyFactory)
        {
            Name = name;
            Pattern = pattern ?? string.Empty;
            DataTokens = dataTokens;

            try
            {
                // Data we parse from the pattern will be used to fill in the rest of the constraints or
                // defaults. The parser will throw for invalid routes.
                ParsedPattern = RoutePatternFactory.Parse(pattern, defaults, constraints);
                Constraints = BuildConstraints(parameterPolicyFactory);

                Defaults = defaults;
                // Merge defaults outside of RoutePattern because the defaults will already have values from pattern
                MergedDefaults = new RouteValueDictionary(ParsedPattern.Defaults);
            }
            catch (Exception exception)
            {
                throw new RouteCreationException(
                    string.Format(CultureInfo.CurrentCulture, "An error occurred while creating the route with name '{0}' and pattern '{1}'.", name, pattern), exception);
            }
        }

        public string Name { get; }
        public string Pattern { get; }

        // Non-inline defaults
        public RouteValueDictionary Defaults { get; }

        // Inline and non-inline defaults merged into one
        public RouteValueDictionary MergedDefaults { get; }

        public IDictionary<string, IList<IRouteConstraint>> Constraints { get; }
        public RouteValueDictionary DataTokens { get; }
        public RoutePattern ParsedPattern { get; private set; }

        private Dictionary<string, IList<IRouteConstraint>> BuildConstraints(ParameterPolicyFactory parameterPolicyFactory)
        {
            var constraints = new Dictionary<string, IList<IRouteConstraint>>(StringComparer.OrdinalIgnoreCase);

            foreach (var parameter in ParsedPattern.Parameters)
            {
                foreach (var parameterPolicy in parameter.ParameterPolicies)
                {
                    var createdPolicy = parameterPolicyFactory.Create(parameter, parameterPolicy);
                    if (createdPolicy is IRouteConstraint routeConstraint)
                    {
                        if (!constraints.TryGetValue(parameter.Name, out var paramConstraints))
                        {
                            paramConstraints = new List<IRouteConstraint>();
                            constraints.Add(parameter.Name, paramConstraints);
                        }

                        paramConstraints.Add(routeConstraint);
                    }
                }
            }

            return constraints;
        }
    }
}
