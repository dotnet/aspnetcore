// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNet.Mvc.ApplicationModel;

namespace ApiExplorer
{
    // Enables ApiExplorer for controllers that haven't explicitly configured it.
    // This is part of the test that validates that ApiExplorer can be configured via
    // convention
    public class ApiExplorerVisibilityEnabledConvention : IGlobalModelConvention
    {
        public void Apply(GlobalModel model)
        {
            foreach (var controller in model.Controllers)
            {
                if (controller.ApiExplorerIsVisible == null)
                {
                    controller.ApiExplorerIsVisible = true;
                    controller.ApiExplorerGroupName = controller.ControllerName;
                }
            }
        }
    }
}