// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components.HotReload;
using Microsoft.AspNetCore.Mvc;

namespace ComponentsApp.Server;

[ApiController]
public class ReloadController : ControllerBase
{
    [HttpGet("/rerender")]
    public IActionResult Rerender()
    {
        HotReloadManager.UpdateApplication(default);

        return Ok();
    }
}
