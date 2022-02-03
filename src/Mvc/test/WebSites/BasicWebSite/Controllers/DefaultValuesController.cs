// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;
using Microsoft.AspNetCore.Mvc;

namespace BasicWebSite.Controllers;

public class DefaultValuesController : Controller
{
    [HttpGet]
    public string EchoValue_DefaultValueAttribute([DefaultValue("hello")] string input)
    {
        return input;
    }

    [HttpGet]
    public string EchoValue_DefaultParameterValue(string input = "world")
    {
        return input;
    }

    [HttpGet]
    public string EchoValue_DefaultParameterValue_ForStructs(
        Guid guid = default(Guid),
        TimeSpan timeSpan = default(TimeSpan))
    {
        return $"{guid}, {timeSpan}";
    }

    [HttpGet]
    [Route("/[controller]/EchoValue_DefaultParameterValue_ForGlobbedPath/{**path}")]
    public string EchoValue_DefaultParameterValue_ForGlobbedPath(string path = "index.html")
    {
        return path;
    }
}
