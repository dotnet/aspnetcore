// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Mvc;
using XmlFormattersWebSite.Models;

namespace XmlFormattersWebSite.Controllers;

public class SerializableErrorController : Controller
{
    [HttpGet]
    public IActionResult ModelStateErrors()
    {
        InvalidOperationException exception = null;

        try
        {
            throw new InvalidOperationException("Error in executing the action");
        }
        catch (InvalidOperationException invalidOperationEx)
        {
            exception = invalidOperationEx;
        }

        ModelState.AddModelError("key1", "key1-error");
        ModelState.AddModelError("key2", exception, ViewData.ModelMetadata);

        return new ObjectResult(new SerializableError(ModelState));
    }

    [HttpPost]
    public SerializableError LogErrors([FromBody] SerializableError serializableError)
    {
        return serializableError;
    }

    [HttpPost]
    public IActionResult CreateEmployee([FromBody] Employee employee)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        return Content("Hello World!");
    }
}
