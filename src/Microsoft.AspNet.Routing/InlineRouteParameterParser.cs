// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text.RegularExpressions;
using Microsoft.AspNet.Routing.Template;

namespace Microsoft.AspNet.Routing
{
    public static class InlineRouteParameterParser
    {
        // One or more characters, matches "id"
        private const string ParameterNamePattern = @"(?<parameterName>.+?)";

        // Zero or more inline constraints that start with a colon followed by zero or more characters
        // Optionally the constraint can have arguments within parentheses
        //      - necessary to capture characters like ":" and "}"
        // Matches ":int", ":length(2)", ":regex(\})", ":regex(:)" zero or more times
        private const string ConstraintPattern = @"(:(?<constraint>.*?(\(.*?\))?))*";

        // A default value with an equal sign followed by zero or more characters
        // Matches "=", "=abc"
        private const string DefaultValueParameter = @"(?<defaultValue>(=.*?))?";

        private static readonly Regex _parameterRegex = new Regex(
           "^" + ParameterNamePattern + ConstraintPattern + DefaultValueParameter + "$",
            RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);

        public static TemplatePart ParseRouteParameter([NotNull] string routeParameter)
        {
            var isCatchAll = routeParameter.StartsWith("*", StringComparison.Ordinal);
            var isOptional = routeParameter.EndsWith("?", StringComparison.Ordinal);

            routeParameter = isCatchAll ? routeParameter.Substring(1) : routeParameter;
            routeParameter = isOptional ? routeParameter.Substring(0, routeParameter.Length - 1) : routeParameter;

            var parameterMatch = _parameterRegex.Match(routeParameter);
            if (!parameterMatch.Success)
            {
                return TemplatePart.CreateParameter(name: string.Empty,
                                                    isCatchAll: isCatchAll,
                                                    isOptional: isOptional,
                                                    defaultValue: null,
                                                    inlineConstraints: null);
            }

            var parameterName = parameterMatch.Groups["parameterName"].Value;

            // Add the default value if present
            var defaultValueGroup = parameterMatch.Groups["defaultValue"];
            var defaultValue = GetDefaultValue(defaultValueGroup);

            // Register inline constraints if present
            var constraintGroup = parameterMatch.Groups["constraint"];
            var inlineConstraints = GetInlineConstraints(constraintGroup);

            return TemplatePart.CreateParameter(parameterName,
                                                isCatchAll,
                                                isOptional,
                                                defaultValue,
                                                inlineConstraints);
        }

        private static string GetDefaultValue(Group defaultValueGroup)
        {
            if (defaultValueGroup.Success)
            {
                var defaultValueMatch = defaultValueGroup.Value;

                // Strip out the equal sign at the beginning
                Debug.Assert(defaultValueMatch.StartsWith("=", StringComparison.Ordinal));
                return defaultValueMatch.Substring(1);
            }

            return null;
        }

        private static IEnumerable<InlineConstraint> GetInlineConstraints(Group constraintGroup)
        {
            var constraints = new List<InlineConstraint>();

            foreach (Capture capture in constraintGroup.Captures)
            {
                constraints.Add(new InlineConstraint(capture.Value));
            }

            return constraints;
        }
    }
}