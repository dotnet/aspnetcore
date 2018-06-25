// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Globalization;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Routing.Template;

namespace Microsoft.AspNetCore.Builder
{
    public class MvcEndpointInfo
    {
        public MvcEndpointInfo(
            string name,
            string template,
            RouteValueDictionary defaults,
            IDictionary<string, object> constraints,
            RouteValueDictionary dataTokens,
            IInlineConstraintResolver constraintResolver)
        {
            Name = name;
            Template = template ?? string.Empty;
            DataTokens = dataTokens;

            try
            {
                // Data we parse from the template will be used to fill in the rest of the constraints or
                // defaults. The parser will throw for invalid routes.
                ParsedTemplate = TemplateParser.Parse(template);

                Constraints = GetConstraints(constraintResolver, ParsedTemplate, constraints);
                Defaults = defaults;
                MergedDefaults = GetDefaults(ParsedTemplate, defaults);
            }
            catch (Exception exception)
            {
                throw new RouteCreationException(
                    string.Format(CultureInfo.CurrentCulture, "An error occurred while creating the route with name '{0}' and template '{1}'.", name, template), exception);
            }
        }

        public string Name { get; }
        public string Template { get; }

        // Non-inline defaults
        public RouteValueDictionary Defaults { get; }

        // Inline and non-inline defaults merged into one
        public RouteValueDictionary MergedDefaults { get; }

        public IDictionary<string, IRouteConstraint> Constraints { get; }
        public RouteValueDictionary DataTokens { get; }
        internal RouteTemplate ParsedTemplate { get; private set; }

        private static IDictionary<string, IRouteConstraint> GetConstraints(
            IInlineConstraintResolver inlineConstraintResolver,
            RouteTemplate parsedTemplate,
            IDictionary<string, object> constraints)
        {
            var constraintBuilder = new RouteConstraintBuilder(inlineConstraintResolver, parsedTemplate.TemplateText);

            if (constraints != null)
            {
                foreach (var kvp in constraints)
                {
                    constraintBuilder.AddConstraint(kvp.Key, kvp.Value);
                }
            }

            foreach (var parameter in parsedTemplate.Parameters)
            {
                if (parameter.IsOptional)
                {
                    constraintBuilder.SetOptional(parameter.Name);
                }

                foreach (var inlineConstraint in parameter.InlineConstraints)
                {
                    constraintBuilder.AddResolvedConstraint(parameter.Name, inlineConstraint.Constraint);
                }
            }

            return constraintBuilder.Build();
        }

        private static RouteValueDictionary GetDefaults(
            RouteTemplate parsedTemplate,
            RouteValueDictionary defaults)
        {
            var result = defaults == null ? new RouteValueDictionary() : new RouteValueDictionary(defaults);

            foreach (var parameter in parsedTemplate.Parameters)
            {
                if (parameter.DefaultValue != null)
                {
                    if (result.ContainsKey(parameter.Name))
                    {
                        throw new InvalidOperationException(
                            string.Format(CultureInfo.CurrentCulture, "The route parameter '{0}' has both an inline default value and an explicit default value specified. A route parameter cannot contain an inline default value when a default value is specified explicitly. Consider removing one of them.", parameter.Name));
                    }
                    else
                    {
                        result.Add(parameter.Name, parameter.DefaultValue);
                    }
                }
            }

            return result;
        }
    }
}
