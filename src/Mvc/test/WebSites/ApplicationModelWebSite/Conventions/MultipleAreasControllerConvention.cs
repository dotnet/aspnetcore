// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection;
using Microsoft.AspNetCore.Mvc.ApplicationModels;

namespace ApplicationModelWebSite;

public class MultipleAreasControllerConvention : IApplicationModelConvention
{
    public void Apply(ApplicationModel application)
    {
        var controllerModels = new List<ControllerModel>();
        foreach (var controller in application.Controllers)
        {
            var areaNames = controller.ControllerType.GetCustomAttributes<MultipleAreasAttribute>()?.FirstOrDefault()?.AreaNames;
            controller.RouteValues.Add("area", areaNames?[0]);
            for (var i = 1; i < areaNames?.Length; i++)
            {
                var controllerCopy = new ControllerModel(controller);
                controllerCopy.RouteValues["area"] = areaNames[i];
                controllerModels.Add(controllerCopy);
            }
        }

        foreach (var model in controllerModels)
        {
            application.Controllers.Add(model);
        }
    }
}
