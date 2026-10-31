// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Mvc;

namespace ApplicationModelWebSite;

// This controller uses an reflected model attribute to add arbitrary data to controller and action model.
[ControllerDescription("Common Controller Description")]
public class ApplicationModelController : Controller
{
    public string GetControllerDescription()
    {
        return ControllerContext.ActionDescriptor.Properties["description"].ToString();
    }

    [ActionDescription("Specific Action Description")]
    public string GetActionSpecificDescription()
    {
        return ControllerContext.ActionDescriptor.Properties["description"].ToString();
    }
}
