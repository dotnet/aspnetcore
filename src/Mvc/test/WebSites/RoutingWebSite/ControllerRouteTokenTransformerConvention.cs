// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Linq;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.AspNetCore.Routing;

namespace RoutingWebSite
{
    public class ControllerRouteTokenTransformerConvention : RouteTokenTransformerConvention
    {
        private readonly Type _controllerType;

        public ControllerRouteTokenTransformerConvention(Type controllerType, IOutboundParameterTransformer parameterTransformer)
            : base(parameterTransformer)
        {
            if (parameterTransformer == null)
            {
                throw new ArgumentNullException(nameof(parameterTransformer));
            }

            _controllerType = controllerType;
        }

        protected override bool ShouldApply(ActionModel action)
        {
            return action.Controller.ControllerType == _controllerType;
        }
    }
}
