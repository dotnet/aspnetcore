// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Mvc;

namespace FormatterWebSite.Controllers;

public class RespectBrowserAcceptHeaderController : Controller
{
    [HttpGet]
    public string ReturnString()
    {
        return "Hello World!";
    }
}
