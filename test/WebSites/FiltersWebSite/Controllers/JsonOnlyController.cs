// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using Microsoft.AspNet.Mvc;

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

        private class JsonOnlyAttribute : Attribute, IResourceFilter
        {
            public void OnResourceExecuted(ResourceExecutedContext context)
            {
            }

            public void OnResourceExecuting(ResourceExecutingContext context)
            {
                // InputFormatters collection contains JsonInputFormatter and JsonPatchInputFormatter. Picking
                // JsonInputFormatter by matching the typename
                var jsonFormatter = context.InputFormatters.OfType<JsonInputFormatter>()
                    .Where(t => t.GetType() == typeof(JsonInputFormatter)).FirstOrDefault();

                context.InputFormatters.Clear();
                context.InputFormatters.Add(jsonFormatter);

                // Update the output formatter collection to only return JSON. 
                var jsonOutputFormatter = context.OutputFormatters.OfType<JsonOutputFormatter>().Single();
                context.OutputFormatters.Clear();
                context.OutputFormatters.Add(jsonOutputFormatter);
            }
        }
    }
}