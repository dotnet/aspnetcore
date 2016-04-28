// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Mvc;

namespace ApplicationModelWebSite.Controllers
{
    public class LicenseController : Controller
    {
        [HttpGet("License/GetLicense")]
        public string GetLicense()
        {
            return ControllerContext.ActionDescriptor.Properties["license"].ToString();
        }
    }
}