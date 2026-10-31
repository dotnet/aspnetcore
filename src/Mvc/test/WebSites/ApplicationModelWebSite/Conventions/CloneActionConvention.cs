// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Mvc.ApplicationModels;

namespace ApplicationModelWebSite;

public class CloneActionConvention : IControllerModelConvention
{
    public void Apply(ControllerModel controller)
    {
        var actionModels = new List<ActionModel>();
        foreach (var action in controller.Actions)
        {
            var actionName = action.Attributes.OfType<CloneActionAttribute>()?.FirstOrDefault()?.ActionName;

            if (!string.IsNullOrEmpty(actionName))
            {
                var actionCopy = new ActionModel(action)
                {
                    ActionName = actionName
                };

                actionModels.Add(actionCopy);
            }
        }

        foreach (var model in actionModels)
        {
            controller.Actions.Add(model);
        }
    }
}
