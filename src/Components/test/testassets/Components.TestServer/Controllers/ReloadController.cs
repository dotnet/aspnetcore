// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Mvc;
using TestServer;

namespace ComponentsApp.Server;

[ApiController]
public class ReloadController : ControllerBase
{
    [HttpGet("/rerender")]
    public IActionResult Rerender()
    {
        ComponentsTestHooks.TriggerHotReloadForTest();

        return Ok();
    }
}
