// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Reflection;
using Microsoft.AspNet.Mvc.ApplicationModel;

namespace ApiExplorer
{
    // Disables ApiExplorer for a specific controller type.
    // This is part of the test that validates that ApiExplorer can be configured via
    // convention
    public class ApiExplorerVisibilityDisabledConvention : IGlobalModelConvention
    {
        private readonly TypeInfo _type;

        public ApiExplorerVisibilityDisabledConvention(Type type)
        {
            _type = type.GetTypeInfo();
        }

        public void Apply(GlobalModel model)
        {
            foreach (var controller in model.Controllers)
            {
                if (controller.ControllerType == _type)
                {
                    controller.ApiExplorerIsVisible = false;
                }
            }
        }
    }
}