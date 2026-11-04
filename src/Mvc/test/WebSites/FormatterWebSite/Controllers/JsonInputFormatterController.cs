// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Mvc;

namespace FormatterWebSite.Controllers;

[ApiController]
[Route("[controller]/[action]")]
public class JsonInputFormatterController
{
    [HttpPost]
    public ActionResult<int> IntValue(int value)
    {
        return value;
    }
}
