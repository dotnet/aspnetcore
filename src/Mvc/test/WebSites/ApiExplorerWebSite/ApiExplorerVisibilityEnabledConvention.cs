// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Mvc.ApplicationModels;

namespace ApiExplorerWebSite;

// Enables ApiExplorer for controllers that haven't explicitly configured it.
// This is part of the test that validates that ApiExplorer can be configured via
// convention
public class ApiExplorerVisibilityEnabledConvention : IApplicationModelConvention
{
    public void Apply(ApplicationModel application)
    {
        foreach (var controller in application.Controllers)
        {
            if (controller.ApiExplorer.IsVisible == null)
            {
                controller.ApiExplorer.IsVisible = true;
                controller.ApiExplorer.GroupName = controller.ControllerName;
            }
        }
    }
}
