// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using BasicWebSite.Formatters;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace BasicWebSite.Controllers.ContentNegotiation;

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
