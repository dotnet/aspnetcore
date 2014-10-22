// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNet.Mvc;

namespace FiltersWebSite
{
    [ControllerExceptionFilter]
    public class ExceptionOrderController : Controller, IExceptionFilter
    {
        [HandleInvalidOperationExceptionFilter]
        public string GetError(string error)
        {
            throw new InvalidOperationException(error);
        }

        public void OnException(ExceptionContext context)
        {
            if (context.Exception.GetType() == typeof(InvalidOperationException))
            {
                context.Result = Helpers.GetContentResult(context.Result, "OnException implemented in Controller");
            }
        }
    }
}