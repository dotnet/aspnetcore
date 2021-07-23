// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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