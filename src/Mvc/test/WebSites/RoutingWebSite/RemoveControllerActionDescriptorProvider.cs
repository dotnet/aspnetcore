// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Controllers;

namespace RoutingWebSite;

public class RemoveControllerActionDescriptorProvider : IActionDescriptorProvider
{
    private readonly ControllerToRemove[] _controllerTypes;

    public RemoveControllerActionDescriptorProvider(params ControllerToRemove[] controllerTypes)
    {
        _controllerTypes = controllerTypes;
    }

    public int Order => int.MaxValue;

    public void OnProvidersExecuted(ActionDescriptorProviderContext context)
    {
    }

    public void OnProvidersExecuting(ActionDescriptorProviderContext context)
    {
        foreach (var item in context.Results.ToList())
        {
            if (item is ControllerActionDescriptor controllerActionDescriptor)
            {
                var controllerToRemove = _controllerTypes.SingleOrDefault(c => c.ControllerType == controllerActionDescriptor.ControllerTypeInfo);
                if (controllerToRemove != null)
                {
                    if (controllerToRemove.Actions == null || controllerToRemove.Actions.Contains(controllerActionDescriptor.ActionName))
                    {
                        context.Results.Remove(item);
                    }
                }
            }
        }
    }
}

public class ControllerToRemove
{
    public Type ControllerType { get; set; }
    public string[] Actions { get; set; }
}
