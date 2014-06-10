// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
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
        public static TemplatePart ParseRouteParameter([NotNull] string routeParameter,
                                                       [NotNull] IInlineConstraintResolver constraintResolver)
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
                                                    inlineConstraint: null);
            }

            var parameterName = parameterMatch.Groups["parameterName"].Value;
            
            // Add the default value if present
            var defaultValueGroup = parameterMatch.Groups["defaultValue"];
            var defaultValue = GetDefaultValue(defaultValueGroup);

            // Register inline constraints if present
            var constraintGroup = parameterMatch.Groups["constraint"];
            var inlineConstraint = GetInlineConstraint(constraintGroup, constraintResolver);
            
            return TemplatePart.CreateParameter(parameterName,
                                                isCatchAll,
                                                isOptional,
                                                defaultValue,
                                                inlineConstraint); 
        }

        private static string GetDefaultValue(Group defaultValueGroup)
        {
            if (defaultValueGroup.Success)
            {
                var defaultValueMatch = defaultValueGroup.Value;

                // Strip out the equal sign at the beginning
                Contract.Assert(defaultValueMatch.StartsWith("=", StringComparison.Ordinal));
                return defaultValueMatch.Substring(1);
            }

            return null;
        }

        private static IRouteConstraint GetInlineConstraint(Group constraintGroup,
                                                            IInlineConstraintResolver _constraintResolver)
        {
            var parameterConstraints = new List<IRouteConstraint>();
            foreach (Capture constraintCapture in constraintGroup.Captures)
            {
                var inlineConstraint = constraintCapture.Value;
                var constraint = _constraintResolver.ResolveConstraint(inlineConstraint);
                if (constraint == null)
                {
                    throw new InvalidOperationException(
                        Resources.FormatInlineRouteParser_CouldNotResolveConstraint(
                                        _constraintResolver.GetType().Name, inlineConstraint));
                }

                parameterConstraints.Add(constraint);
            }

            if (parameterConstraints.Count > 0)
            {
                var constraint = parameterConstraints.Count == 1 ?
                                            parameterConstraints[0] :
                                            new CompositeRouteConstraint(parameterConstraints);
                return constraint;
            }

            return null;
        }
    }
}