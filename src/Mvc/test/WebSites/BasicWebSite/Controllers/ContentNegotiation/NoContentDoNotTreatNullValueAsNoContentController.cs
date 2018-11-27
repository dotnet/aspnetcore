// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.Formatters;

namespace BasicWebSite.Controllers.ContentNegotiation
{
    public class NoContentDoNotTreatNullValueAsNoContentController : Controller
    {
        public override void OnActionExecuted(ActionExecutedContext context)
        {
            var result = context.Result as ObjectResult;
            if (result != null)
            {
                var noContentFormatter = new HttpNoContentOutputFormatter() { TreatNullValueAsNoContent = false };
                result.Formatters.Add(noContentFormatter);
            }

            base.OnActionExecuted(context);
        }

        public Task<string> ReturnTaskOfString_NullValue()
        {
            return Task.FromResult<string>(null);
        }

        public Task<object> ReturnTaskOfObject_NullValue()
        {
            return Task.FromResult<object>(null);
        }

        public object ReturnObject_NullValue()
        {
            return null;
        }

        public string ReturnString_NullValue()
        {
            return null;
        }
    }
}