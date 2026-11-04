// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection;
using Microsoft.AspNetCore.Mvc.ApplicationModels;

namespace ApiExplorerWebSite;

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
