// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNet.Http;
using Microsoft.AspNet.Mvc;
using Microsoft.AspNet.Mvc.HeaderValueAbstractions;
using Microsoft.Framework.DependencyInjection;
namespace ConnegWebsite
{
    public class FallbackOnTypeBasedMatchController : Controller
    {
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
            objectResult.Formatters.Add(new JsonOutputFormatter());

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
            objectResult.Formatters.Add(new HttpNotAcceptableOutputFormatter());
            objectResult.Formatters.Add(new PlainTextFormatter());
            objectResult.Formatters.Add(new JsonOutputFormatter());
            return objectResult;
        }

        public IActionResult OverrideTheFallback_WithDefaultFormatters(int input)
        {
            var objectResult = new ObjectResult(input);
            var formattersProvider = ActionContext.HttpContext.RequestServices.GetRequiredService<IOutputFormattersProvider>();
            objectResult.Formatters.Add(new HttpNotAcceptableOutputFormatter());
            foreach (var formatter in formattersProvider.OutputFormatters)
            {
                objectResult.Formatters.Add(formatter);
            }

            return objectResult;
        }
    }
}