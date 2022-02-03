// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApplicationModels;

namespace ApplicationModelWebSite;

// This controller uses an reflected model attribute to change an action name, and thus
// the URL.
public class ActionModelController : Controller
{
    [ActionName2("ActionName")]
    public string GetActionName()
    {
        return ControllerContext.ActionDescriptor.ActionName;
    }

    [CloneAction("MoreHelp")]
    public IActionResult Help()
    {
        return View();
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
