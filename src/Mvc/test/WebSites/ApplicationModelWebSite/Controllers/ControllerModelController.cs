// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.AspNetCore.Mvc.Controllers;

namespace ApplicationModelWebSite
{
    // This controller uses an reflected model attribute to change the controller name, and thus
    // the URL.
    [ControllerName("CoolController")]
    public class ControllerModelController : Controller
    {
        public string GetControllerName()
        {
            return ControllerContext.ActionDescriptor.ControllerName;
        }

        private class ControllerNameAttribute : Attribute, IControllerModelConvention
        {
            private readonly string _controllerName;

            public ControllerNameAttribute(string controllerName)
            {
                _controllerName = controllerName;
            }

            public void Apply(ControllerModel model)
            {
                model.ControllerName = _controllerName;
            }
        }
    }
}