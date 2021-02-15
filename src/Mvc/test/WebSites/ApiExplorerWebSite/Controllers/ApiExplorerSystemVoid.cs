// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Mvc;

namespace ApiExplorerWebSite
{
    [Route("ApiExplorerVoid/[action]")]
    [ApiController]
    public class ApiExplorerVoidController : Controller
    {
        [ProducesResponseType(typeof(void), 401, true)]
        public IActionResult ActionWithVoidType() => Ok();

        [ProducesResponseType(typeof(void), 401)]
        public IActionResult ActionWithVoidTypeAndOverrideDisabled() => Ok();

    }
}
