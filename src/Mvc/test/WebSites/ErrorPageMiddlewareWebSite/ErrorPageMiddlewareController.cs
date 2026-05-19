// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection;
using Microsoft.AspNetCore.Mvc;

namespace ErrorPageMiddlewareWebSite;

public class ErrorPageMiddlewareController : Controller
{
    [HttpGet("/CompilationFailure")]
    public IActionResult CompilationFailure()
    {
        return View();
    }

    [HttpGet("/ParserError")]
    public IActionResult ParserError()
    {
        return View();
    }

    [HttpGet("/ErrorFromViewImports")]
    public IActionResult ViewImportsError()
    {
        return View("~/Views/ErrorFromViewImports/Index.cshtml");
    }

    [HttpGet("/RuntimeError")]
    public IActionResult RuntimeError() => View();

    [HttpGet("/LoaderException")]
    public IActionResult ReflectionTypeLoadException()
    {
        throw new ReflectionTypeLoadException(
            new[] { typeof(SomeType) },
            new[] { new TypeLoadException("Custom Loader Exception.") });
    }

    private class SomeType
    {
    }
}
