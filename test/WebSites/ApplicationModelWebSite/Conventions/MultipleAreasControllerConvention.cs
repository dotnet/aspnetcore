// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.AspNetCore.Mvc.ApplicationModels;

namespace ApplicationModelWebSite
{
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
}
