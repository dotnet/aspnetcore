// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Mvc.ApplicationModels;

namespace RoutingWebSite;

public class ControllerRouteTokenTransformerConvention : RouteTokenTransformerConvention
{
    private readonly Type _controllerType;

    public ControllerRouteTokenTransformerConvention(Type controllerType, IOutboundParameterTransformer parameterTransformer)
        : base(parameterTransformer)
    {
        ArgumentNullException.ThrowIfNull(parameterTransformer);

        _controllerType = controllerType;
    }

    protected override bool ShouldApply(ActionModel action)
    {
        return action.Controller.ControllerType == _controllerType;
    }
}
