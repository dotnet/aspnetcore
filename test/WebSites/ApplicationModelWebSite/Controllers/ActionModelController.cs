// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNet.Mvc;
using Microsoft.AspNet.Mvc.ApplicationModel;

namespace ApplicationModelWebSite
{
    // This controller uses an reflected model attribute to change an action name, and thus
    // the URL.
    public class ActionModelController : Controller
    {
        [ActionName2("ActionName")]
        public string GetActionName()
        {
            var actionDescriptor = (ControllerActionDescriptor)ActionContext.ActionDescriptor;

            return actionDescriptor.Name;
        }

        private class ActionName2Attribute : Attribute, IActionModelConvention
        {
            private readonly string _actionName;

            public ActionName2Attribute(string actionName)
            {
                _actionName = actionName;
            }

            public void Apply(ActionModel model)
            {
                model.ActionName = _actionName;
            }
        }
    }
}