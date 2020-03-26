// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace SecurityWebSite.Controllers
{
    [Authorize] // requires any authenticated user (aka the application cookie typically)
    public class AuthorizedController : ControllerBase
    {
        [Authorize(AuthenticationSchemes = "Bearer")]
        public IActionResult Api() => Ok();

        public IActionResult Cookie() => Ok();

        [AllowAnonymous]
        public IActionResult AllowAnonymous() => Ok();
    }
}
