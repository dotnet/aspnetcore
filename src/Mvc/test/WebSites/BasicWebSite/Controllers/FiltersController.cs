// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using BasicWebSite.Models;
using Microsoft.AspNetCore.Mvc;

namespace BasicWebSite.Controllers;

public class FiltersController : Controller
{
    [HttpPost]
    [Consumes("application/yaml")]
    [UnprocessableResultFilter]
    public IActionResult AlwaysRunResultFiltersCanRunWhenResourceFilterShortCircuit([FromBody] Product product) =>
        throw new Exception("Shouldn't be executed");

    [ServiceFilter<ServiceActionFilter>]
    public IActionResult ServiceFilterTest() => Content("Service filter content");

    [TraceResultOutputFilter]
    public IActionResult TraceResult() => new EmptyResult();

    [Route("{culture}/[controller]/[action]")]
    [MiddlewareFilter<LocalizationPipeline>]
    public IActionResult MiddlewareFilterTest()
    {
        return Content($"CurrentCulture:{CultureInfo.CurrentCulture.Name},CurrentUICulture:{CultureInfo.CurrentUICulture.Name}");
    }
}
