// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Mvc;

namespace ApiExplorerWebSite
{
    [Route("ApiExplorerReload")]
    public class ApiExplorerReloadableController : Controller
    {
        [Route("Index")]
        [Reload]
        public string Index() => "Hello world";

        [Route("Reload")]
        [PassThru]
        public IActionResult Reload([FromServices] WellKnownChangeToken changeToken)
        {
            changeToken.TokenSource.Cancel();
            return Ok();
        }
    }
}
