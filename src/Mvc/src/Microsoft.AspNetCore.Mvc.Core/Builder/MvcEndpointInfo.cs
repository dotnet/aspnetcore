// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Globalization;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Routing.Patterns;

namespace Microsoft.AspNetCore.Builder
{
    internal class MvcEndpointInfo : DefaultEndpointConventionBuilder
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
                ParameterPolicies = BuildParameterPolicies(ParsedPattern.Parameters, parameterPolicyFactory);

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

        public IDictionary<string, IList<IParameterPolicy>> ParameterPolicies { get; }
        public RouteValueDictionary DataTokens { get; }
        public RoutePattern ParsedPattern { get; private set; }

        internal static Dictionary<string, IList<IParameterPolicy>> BuildParameterPolicies(IReadOnlyList<RoutePatternParameterPart> parameters, ParameterPolicyFactory parameterPolicyFactory)
        {
            var policies = new Dictionary<string, IList<IParameterPolicy>>(StringComparer.OrdinalIgnoreCase);

            foreach (var parameter in parameters)
            {
                foreach (var parameterPolicy in parameter.ParameterPolicies)
                {
                    var createdPolicy = parameterPolicyFactory.Create(parameter, parameterPolicy);
                    if (!policies.TryGetValue(parameter.Name, out var policyList))
                    {
                        policyList = new List<IParameterPolicy>();
                        policies.Add(parameter.Name, policyList);
                    }

                    policyList.Add(createdPolicy);
                }
            }

            return policies;
        }
    }
}