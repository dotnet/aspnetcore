// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNet.Mvc;
using Microsoft.AspNet.Mvc.ReflectedModelBuilder;

namespace ReflectedModelWebSite
{
    // This controller uses an reflected model attribute to change an action name, and thus
    // the URL.
    public class ReflectedActionModelController : Controller
    {
        [ActionName2("ActionName")]
        public string GetActionName()
        {
            var actionDescriptor = (ReflectedActionDescriptor)ActionContext.ActionDescriptor;

            return actionDescriptor.Name;
        }

        private class ActionName2Attribute : Attribute, IReflectedActionModelConvention
        {
            private readonly string _actionName;

            public ActionName2Attribute(string actionName)
            {
                _actionName = actionName;
            }

            public void Apply(ReflectedActionModel model)
            {
                model.ActionName = _actionName;
            }
        }
    }
}