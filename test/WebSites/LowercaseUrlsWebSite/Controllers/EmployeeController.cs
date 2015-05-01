// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNet.Mvc;

namespace LowercaseUrlsWebSite
{
    [Route("api/Employee/[action]/{name?}")]
    public class LowercaseUrls_EmployeeController : Controller
    {
        public string List()
        {
            return Url.Action();
        }

        public string GetEmployee(string name)
        {
            return Url.Action();
        }
    }
}