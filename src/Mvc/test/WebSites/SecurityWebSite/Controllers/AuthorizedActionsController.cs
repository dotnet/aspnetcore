// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace SecurityWebSite.Controllers
{
    public class AuthorizedActionsController : ControllerBase
    {
        [AllowAnonymous]
        public IActionResult ActionWithoutAllowAnonymous() => Ok();

        public IActionResult ActionWithoutAuthAttribute() => Ok();

        [Authorize("RequireClaimB")]
        public IActionResult ActionWithAuthAttribute() => Ok();
    }
}
