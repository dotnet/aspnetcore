// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNet.Mvc;
using Microsoft.AspNet.Mvc.Filters;
using Microsoft.AspNet.Mvc.Formatters;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace BasicWebSite
{
    public class SpecificFormattersController : Controller
    {
        [HttpPost("api/ActionUsingSpecificFormatters")]
        [CamelCaseJsonFormatters]
        public IActionResult ActionUsingSpecificFormatters([FromBody] Person person)
        {
            if (!ModelState.IsValid)
            {
                return HttpBadRequest(ModelState);
            }

            return Ok(person);
        }

        [HttpPost("api/ActionUsingGlobalFormatters")]
        public IActionResult ActionUsingGlobalFormatters([FromBody] Person person)
        {
            if (!ModelState.IsValid)
            {
                return HttpBadRequest(ModelState);
            }

            return Ok(person);
        }

        public class Person
        {
            public string FullName { get; set; }
        }

        [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
        private class CamelCaseJsonFormattersAttribute : Attribute, IResourceFilter, IResultFilter
        {
            private readonly JsonSerializerSettings _serializerSettings;

            public CamelCaseJsonFormattersAttribute()
            {
                _serializerSettings = new JsonSerializerSettings();
                _serializerSettings.ContractResolver = new CamelCasePropertyNamesContractResolver();
            }

            public void OnResourceExecuted(ResourceExecutedContext context)
            {
            }

            public void OnResourceExecuting(ResourceExecutingContext context)
            {
                // Do not modify existing json formatters as they would effect all controllers.
                // Instead remove and add new formatters which only effects the controllers this
                // attribute is decorated on.
                context.InputFormatters.RemoveType<JsonInputFormatter>();
                context.InputFormatters.Add(new JsonInputFormatter(_serializerSettings));
                
            }

            public void OnResultExecuted(ResultExecutedContext context)
            {
            }

            public void OnResultExecuting(ResultExecutingContext context)
            {
                var objectResult = context.Result as ObjectResult;
                if (objectResult != null)
                {
                    objectResult.Formatters.RemoveType<JsonOutputFormatter>();
                    objectResult.Formatters.Add(new JsonOutputFormatter(_serializerSettings));
                }
            }
        }
    }
}
