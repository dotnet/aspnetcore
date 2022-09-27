// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using BasicWebSite.Formatters;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.Extensions.Options;

namespace BasicWebSite.Controllers.ContentNegotiation;

public class FallbackOnTypeBasedMatchController : Controller
{
    private readonly IOptions<MvcOptions> _mvcOptions;
    private readonly NewtonsoftJsonOutputFormatter _jsonOutputFormatter;

    public FallbackOnTypeBasedMatchController(IOptions<MvcOptions> mvcOptions)
    {
        _mvcOptions = mvcOptions;
        _jsonOutputFormatter = mvcOptions.Value.OutputFormatters.OfType<NewtonsoftJsonOutputFormatter>().First();
    }

    public int UseTheFallback_WithDefaultFormatters(int input)
    {
        return input;
    }

    public IActionResult UseTheFallback_UsingCustomFormatters(int input)
    {
        var objectResult = new ObjectResult(input);

        // Request content type is application/custom.
        // PlainTextFormatter cannot write because it does not support the type.
        // JsonOutputFormatter cannot write in the first attempt because it does not support the
        // request content type.
        objectResult.Formatters.Add(new PlainTextFormatter());
        objectResult.Formatters.Add(_jsonOutputFormatter);

        return objectResult;
    }

    public IActionResult FallbackGivesNoMatch(int input)
    {
        var objectResult = new ObjectResult(input);

        // Request content type is application/custom.
        // PlainTextFormatter cannot write because it does not support the type.
        objectResult.Formatters.Add(new PlainTextFormatter());

        return objectResult;
    }

    public IActionResult OverrideTheFallback_UsingCustomFormatters(int input)
    {
        var objectResult = new ObjectResult(input);
        objectResult.Formatters.Add(new PlainTextFormatter());
        objectResult.Formatters.Add(_jsonOutputFormatter);

        return objectResult;
    }

    public IActionResult OverrideTheFallback_WithDefaultFormatters(int input)
    {
        var objectResult = new ObjectResult(input);
        foreach (var formatter in _mvcOptions.Value.OutputFormatters)
        {
            objectResult.Formatters.Add(formatter);
        }

        return objectResult;
    }

    public IActionResult ReturnString(
        [FromServices] IOptions<MvcOptions> optionsAccessor)
    {
        var objectResult = new ObjectResult("Hello World!");

        foreach (var formatter in optionsAccessor.Value.OutputFormatters)
        {
            objectResult.Formatters.Add(formatter);
        }

        return objectResult;
    }
}
