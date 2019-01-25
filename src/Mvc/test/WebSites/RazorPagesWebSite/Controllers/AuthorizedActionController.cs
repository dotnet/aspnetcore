// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace RazorPagesWebSite.Controllers
{
    [Route("[controller]/[action]")]
    [Authorize]
    public class AuthorizedActionController : Controller
    {
        public IActionResult Index() => Ok();
    }
}
