// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace FiltersWebSite.Controllers
{
    [Route("ResourceFilter/[action]")]
    public class ResourceFilterController : Controller
    {
        [HttpPost]
        [ShortCircuitWithFormatter]
        public string Get()
        {
            return "NeverGetsExecuted";
        }

        [HttpPost]
        [ShortCircuit]
        public string Post()
        {
            return "NeverGetsExecuted";
        }

        [HttpPost]
        public IActionResult FormValueModelBinding_Enabled(DummyClass dc)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            return Ok("Data:" + dc?.SampleInt);
        }

        [HttpPost]
        [DisableFormValueModelBinding]
        public IActionResult FormValueModelBinding_Disabled(DummyClass dc)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            return Ok("Data:" + dc?.SampleInt);
        }

        private class ShortCircuitWithFormatterAttribute : Attribute, IResourceFilter
        {
            public void OnResourceExecuted(ResourceExecutedContext context)
            {
            }

            public void OnResourceExecuting(ResourceExecutingContext context)
            {
                var mvcOptions = context.HttpContext.RequestServices.GetRequiredService<IOptions<MvcOptions>>();
                var formatter = mvcOptions.Value.OutputFormatters.OfType<JsonOutputFormatter>().First();
                var result = new ObjectResult("someValue");
                result.Formatters.Add(formatter);

                context.Result = result;
            }
        }

        private class ShortCircuitAttribute : Attribute, IResourceFilter
        {
            public void OnResourceExecuted(ResourceExecutedContext context)
            {
            }

            public void OnResourceExecuting(ResourceExecutingContext context)
            {
                // ShortCircuit.
                context.Result = new ObjectResult("someValue");
            }
        }
    }
}