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
    [JsonOnly]
    [Route("Json")]
    public class JsonOnlyController : Controller
    {
        [HttpPost]
        public string Post([FromBody] DummyClass dummy)
        {
            return (dummy?.SampleInt ?? 0).ToString();
        }

        private class JsonOnlyAttribute : Attribute, IResultFilter
        {
            public void OnResourceExecuted(ResourceExecutedContext context)
            {
            }

            public void OnResourceExecuting(ResourceExecutingContext context)
            {
                // InputFormatters collection contains JsonInputFormatter and JsonPatchInputFormatter. Picking
                // JsonInputFormatter by matching the type exactly rather than using OfType.
                var jsonFormatter = context.InputFormatters.OfType<JsonInputFormatter>()
                    .Where(t => t.GetType() == typeof(JsonInputFormatter)).FirstOrDefault();

                context.InputFormatters.Clear();
                context.InputFormatters.Add(jsonFormatter);
            }

            public void OnResultExecuted(ResultExecutedContext context)
            {
            }

            public void OnResultExecuting(ResultExecutingContext context)
            {
                // Update the output formatter collection to only return JSON. 
                if (context.Result is ObjectResult)
                {
                    var options = context.HttpContext.RequestServices.GetRequiredService<IOptions<MvcOptions>>();
                    var jsonFormatter = options.Value.OutputFormatters.OfType<JsonOutputFormatter>().Single();
                    var result = (ObjectResult)context.Result;
                    result.Formatters.Add(jsonFormatter);
                }
            }
        }
    }
}