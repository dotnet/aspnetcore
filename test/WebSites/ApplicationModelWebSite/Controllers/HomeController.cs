// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNet.Mvc;

namespace ApplicationModelWebSite
{
    public class HomeController : Controller
    {
        public string GetCommonDescription()
        {
            return ControllerContext.ActionDescriptor.Properties["description"].ToString();
        }

        [HttpGet("Home/GetHelloWorld")]
        public object GetHelloWorld([FromHeader] string helloWorld)
        {
            return ControllerContext.ActionDescriptor.Properties["source"].ToString() + " - " + helloWorld;
        }
    }
}