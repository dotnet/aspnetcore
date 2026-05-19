// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Routing.Constraints;
using Microsoft.AspNetCore.Routing.Patterns;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Routing;

internal sealed class DefaultParameterPolicyFactory : ParameterPolicyFactory
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

    public override IParameterPolicy Create(RoutePatternParameterPart? parameter, IParameterPolicy parameterPolicy)
    {
        ArgumentNullException.ThrowIfNull(parameterPolicy);

        if (parameterPolicy is IRouteConstraint routeConstraint)
        {
            return InitializeRouteConstraint(parameter?.IsOptional ?? false, routeConstraint);
        }

        return parameterPolicy;
    }

    public override IParameterPolicy Create(RoutePatternParameterPart? parameter, string inlineText)
    {
        ArgumentNullException.ThrowIfNull(inlineText);

        var parameterPolicy = ParameterPolicyActivator.ResolveParameterPolicy<IParameterPolicy>(
            _options.TrimmerSafeConstraintMap,
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

    private static IParameterPolicy InitializeRouteConstraint(
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
