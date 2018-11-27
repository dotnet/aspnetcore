// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Reflection;
using Microsoft.AspNetCore.Mvc;

namespace ErrorPageMiddlewareWebSite
{
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
}
