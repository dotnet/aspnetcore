// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

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
