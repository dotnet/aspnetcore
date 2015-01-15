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
                var jsonFormatter = context.InputFormatters.OfType<JsonInputFormatter>().Single();

                context.InputFormatters.Clear();
                context.InputFormatters.Add(jsonFormatter);
            }
        }
    }
}