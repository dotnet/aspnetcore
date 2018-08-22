// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Routing.Constraints;
using Microsoft.AspNetCore.Routing.Patterns;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Routing
{
    internal class DefaultParameterPolicyFactory : ParameterPolicyFactory
    {
        private readonly RouteOptions _options;
        private readonly IServiceProvider _serviceProvider;

        public DefaultParameterPolicyFactory(
            IOptions<RouteOptions> options,
            IServiceProvider serviceProvider)
        {
            _options = options.Value;
            _serviceProvider = serviceProvider;
        }

        public override IParameterPolicy Create(RoutePatternParameterPart parameter, IParameterPolicy parameterPolicy)
        {
            if (parameterPolicy == null)
            {
                throw new ArgumentNullException(nameof(parameterPolicy));
            }

            if (parameterPolicy is IRouteConstraint routeConstraint)
            {
                return InitializeRouteConstraint(parameter?.IsOptional ?? false, routeConstraint, argument: null);
            }

            return parameterPolicy;
        }

        public override IParameterPolicy Create(RoutePatternParameterPart parameter, string inlineText)
        {
            if (inlineText == null)
            {
                throw new ArgumentNullException(nameof(inlineText));
            }

            // Example:
            // {productId:regex(\d+)}
            //
            // ParameterName: productId
            // value: regex(\d+)
            // name: regex
            // argument: \d+
            (var name, var argument) = Parse(inlineText);

            if (!_options.ConstraintMap.TryGetValue(name, out var type))
            {
                throw new InvalidOperationException(Resources.FormatRoutePattern_ConstraintReferenceNotFound(
                    name,
                    typeof(RouteOptions),
                    nameof(RouteOptions.ConstraintMap)));
            }

            if (typeof(IRouteConstraint).IsAssignableFrom(type))
            {
                var constraint = DefaultInlineConstraintResolver.CreateConstraint(type, argument);
                return InitializeRouteConstraint(parameter?.IsOptional ?? false, constraint, argument);
            }

            if (typeof(IParameterPolicy).IsAssignableFrom(type))
            {
                var parameterPolicy = (IParameterPolicy)_serviceProvider.GetRequiredService(type);
                return parameterPolicy;
            }

            var message = Resources.FormatRoutePattern_InvalidStringConstraintReference(
                type,
                name,
                typeof(IRouteConstraint),
                typeof(IParameterPolicy));
            throw new InvalidOperationException(message);
        }

        private IParameterPolicy InitializeRouteConstraint(
            bool optional,
            IRouteConstraint routeConstraint,
            string argument)
        {
            if (optional)
            {
                routeConstraint = new OptionalRouteConstraint(routeConstraint);
            }

            return routeConstraint;
        }

        private (string name, string argument) Parse(string text)
        {
            string name;
            string argument;
            var indexOfFirstOpenParens = text.IndexOf('(');
            if (indexOfFirstOpenParens >= 0 && text.EndsWith(")", StringComparison.Ordinal))
            {
                name = text.Substring(0, indexOfFirstOpenParens);
                argument = text.Substring(
                    indexOfFirstOpenParens + 1,
                    text.Length - indexOfFirstOpenParens - 2);
            }
            else
            {
                name = text;
                argument = null;
            }
            return (name, argument);
        }
    }
}
