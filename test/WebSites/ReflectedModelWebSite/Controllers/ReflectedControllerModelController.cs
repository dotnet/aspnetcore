// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNet.Mvc;
using Microsoft.AspNet.Mvc.ReflectedModelBuilder;

namespace ReflectedModelWebSite
{
    // This controller uses an reflected model attribute to change the controller name, and thus
    // the URL.
    [ControllerName("CoolController")]
    public class ReflectedControllerModelController : Controller
    {
        public string GetControllerName()
        {
            var actionDescriptor = (ReflectedActionDescriptor)ActionContext.ActionDescriptor;

            return actionDescriptor.ControllerName;
        }

        private class ControllerNameAttribute : Attribute, IReflectedControllerModelConvention
        {
            private readonly string _controllerName;

            public ControllerNameAttribute(string controllerName)
            {
                _controllerName = controllerName;
            }

            public void Apply(ReflectedControllerModel model)
            {
                model.ControllerName = _controllerName;
            }
        }
    }
}