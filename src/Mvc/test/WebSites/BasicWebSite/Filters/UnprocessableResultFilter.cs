// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace BasicWebSite
{
    public class UnprocessableResultFilter : Attribute, IAlwaysRunResultFilter
    {
        public void OnResultExecuted(ResultExecutedContext context)
        {
        }

        public void OnResultExecuting(ResultExecutingContext context)
        {
            if (context.Result is StatusCodeResult statusCodeResult &&
                statusCodeResult.StatusCode == 415)
            {
                context.Result = new ObjectResult("Can't process this!")
                {
                    StatusCode = 422,
                };
            }
        }
    }
}
