// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Components.HotReload;
using Microsoft.AspNetCore.Mvc;

namespace ComponentsApp.Server
{
    [ApiController]
    public class ReloadController : ControllerBase
    {
        [HttpGet("/rerender")]
        public IActionResult Rerender()
        {
            HotReloadManager.DeltaApplied();

            return Ok();
        }
    }
}
