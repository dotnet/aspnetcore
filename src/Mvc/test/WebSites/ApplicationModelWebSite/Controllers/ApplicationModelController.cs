// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Mvc;

namespace ApplicationModelWebSite
{
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
}