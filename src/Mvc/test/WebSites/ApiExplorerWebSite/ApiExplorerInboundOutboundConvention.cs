// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Reflection;
using ApiExplorerWebSite.Controllers;
using Microsoft.AspNetCore.Mvc.ApplicationModels;

namespace ApiExplorerWebSite
{
    // Disables ApiExplorer for a specific controller type.
    // This is part of the test that validates that ApiExplorer can be configured via
    // convention
    public class ApiExplorerInboundOutboundConvention : IApplicationModelConvention
    {
        private readonly TypeInfo _type;

        public ApiExplorerInboundOutboundConvention(Type type)
        {
            _type = type.GetTypeInfo();
        }

        public void Apply(ApplicationModel application)
        {
            foreach (var controller in application.Controllers)
            {
                if (controller.ControllerType == _type)
                {
                    foreach (var action in controller.Actions)
                    {
                        if (action.ActionName == nameof(ApiExplorerInboundOutBoundController.SuppressedForPathMatching))
                        {
                            action.Selectors[0].AttributeRouteModel.SuppressPathMatching = true;
                        }
                        else if (action.ActionName == nameof(ApiExplorerInboundOutBoundController.SuppressedForLinkGeneration))
                        {
                            action.Selectors[0].AttributeRouteModel.SuppressLinkGeneration = true;
                        }
                    }
                }
            }
        }
    }
}