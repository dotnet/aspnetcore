// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Routing.Constraints;
using Microsoft.AspNetCore.Routing.Patterns;
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
                return InitializeRouteConstraint(parameter?.IsOptional ?? false, routeConstraint);
            }

            return parameterPolicy;
        }

        public override IParameterPolicy Create(RoutePatternParameterPart parameter, string inlineText)
        {
            if (inlineText == null)
            {
                throw new ArgumentNullException(nameof(inlineText));
            }

            var parameterPolicy = ParameterPolicyActivator.ResolveParameterPolicy<IParameterPolicy>(
                _options.ConstraintMap,
                _serviceProvider,
                inlineText,
                out var parameterPolicyKey);

            if (parameterPolicy == null)
            {
                throw new InvalidOperationException(Resources.FormatRoutePattern_ConstraintReferenceNotFound(
                        parameterPolicyKey,
                        typeof(RouteOptions),
                        nameof(RouteOptions.ConstraintMap)));
            }

            if (parameterPolicy is IRouteConstraint constraint)
            {
                return InitializeRouteConstraint(parameter?.IsOptional ?? false, constraint);
            }

            return parameterPolicy;
        }

        private IParameterPolicy InitializeRouteConstraint(
            bool optional,
            IRouteConstraint routeConstraint)
        {
            if (optional)
            {
                routeConstraint = new OptionalRouteConstraint(routeConstraint);
            }

            return routeConstraint;
        }
    }
}
