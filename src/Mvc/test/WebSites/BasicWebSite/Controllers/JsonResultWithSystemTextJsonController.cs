// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;
using Microsoft.AspNetCore.Mvc;

namespace BasicWebSite.Controllers;

public class JsonResultWithSystemTextJsonController : Controller
{
    private static readonly JsonSerializerOptions _customSerializerSettings;

    static JsonResultWithSystemTextJsonController()
    {
        _customSerializerSettings = new JsonSerializerOptions();
    }

    public JsonResult Plain()
    {
        return new JsonResult(new { Message = "hello" });
    }

    public JsonResult CustomContentType()
    {
        var result = new JsonResult(new { Message = "hello" });
        result.ContentType = "application/message+json";
        return result;
    }

    public JsonResult CustomSerializerSettings()
    {
        return new JsonResult(new { Message = "hello" }, _customSerializerSettings);
    }

    public JsonResult Null()
    {
        return new JsonResult(null);
    }

    public JsonResult String()
    {
        return new JsonResult("hello");
    }
}
