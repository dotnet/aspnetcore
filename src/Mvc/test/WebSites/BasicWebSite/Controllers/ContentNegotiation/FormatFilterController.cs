// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using BasicWebSite.Formatters;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace BasicWebSite.Controllers.ContentNegotiation
{
    public class FormatFilterController : Controller
    {
        [Produces("application/FormatFilterController")]
        [FormatFilter]
        [CustomFormatterActionFilter]
        public string ProducesTakesPrecedenceOverUserSuppliedFormatMethod()
        {
            return "MethodWithFormatFilter";
        }

        [HttpGet]
        [FormatFilter]
        public Customer CustomerInfo()
        {
            return new Customer() { Name = "John" };
        }

        private class CustomFormatterActionFilter : ActionFilterAttribute
        {
            public override void OnActionExecuted(ActionExecutedContext context)
            {
                var result = context.Result as ObjectResult;
                if (result != null)
                {
                    result.Formatters.Add(new CustomFormatter("application/FormatFilterController"));
                }
            }
        }

        public class Customer
        {
            public string Name { get; set; }
        }
    }

}