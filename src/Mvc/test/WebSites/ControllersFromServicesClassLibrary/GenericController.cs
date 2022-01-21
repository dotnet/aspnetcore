// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Mvc;

namespace ControllersFromServicesClassLibrary;

public class GenericController<TController> : Controller
{
    [HttpGet("/not-discovered/generic")]
    public IActionResult Index()
    {
        return new EmptyResult();
    }
}
