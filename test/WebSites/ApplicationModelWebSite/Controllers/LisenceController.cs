// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNet.Mvc;

namespace ApplicationModelWebSite.Controllers
{
    public class LisenceController : Controller
    {
        [HttpGet("Lisence/GetLisence")]
        public string GetLisence()
        {
            return ControllerContext.ActionDescriptor.Properties["lisence"].ToString();
        }
    }
}