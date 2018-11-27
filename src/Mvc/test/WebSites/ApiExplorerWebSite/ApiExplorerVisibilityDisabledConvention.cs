// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Reflection;
using Microsoft.AspNetCore.Mvc.ApplicationModels;

namespace ApiExplorerWebSite
{
    // Disables ApiExplorer for a specific controller type.
    // This is part of the test that validates that ApiExplorer can be configured via
    // convention
    public class ApiExplorerVisibilityDisabledConvention : IApplicationModelConvention
    {
        private readonly TypeInfo _type;

        public ApiExplorerVisibilityDisabledConvention(Type type)
        {
            _type = type.GetTypeInfo();
        }

        public void Apply(ApplicationModel application)
        {
            foreach (var controller in application.Controllers)
            {
                if (controller.ControllerType == _type)
                {
                    controller.ApiExplorer.IsVisible = false;
                }
            }
        }
    }
}