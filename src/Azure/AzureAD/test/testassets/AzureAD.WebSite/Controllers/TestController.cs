// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.using Microsoft.AspNetCore.Authorization;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AzureAD.WebSite.Controllers
{
    public class TestController : Controller
    {
        [Authorize]
        [HttpGet("/api/get")]
        public IActionResult Get() => Ok();
    }
}
