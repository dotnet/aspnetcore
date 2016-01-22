// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Mvc;

namespace FiltersWebSite.Controllers
{
    public class HomeController
    {
        [ChangeContentActionFilter]
        [ChangeContentResultFilter]
        public IActionResult GetSampleString()
        {
            return new ContentResult()
            {
                Content = "From Controller"
            };
        }

        [ThrowingResultFilter]
        [HandleInvalidOperationExceptionFilter]
        public IActionResult ThrowExcpetion()
        {
            throw new InvalidOperationException("Controller threw.");
        }

        [HandleExceptionActionFilter]
        [ChangeContentResultFilter]
        public IActionResult ThrowExceptionAndHandleInActionFilter()
        {
            throw new InvalidOperationException("Controller threw.");
        }

        [ControllerActionFilter(Order = 2)]
        [ChangeContentActionFilter(Order = 1)]
        public IActionResult ActionFilterOrder()
        {
            return new ContentResult()
            {
                Content = "Hello World"
            };
        }

        [ControllerResultFilter(Order = 1)]
        [ChangeContentResultFilter(Order = 2)]
        public IActionResult ResultFilterOrder()
        {
            return new ContentResult()
            {
                Content = "Hello World"
            };
        }

        [ThrowingResultFilter]
        public string ThrowingResultFilter()
        {
            return "Throwing Result Filter";
        }

        [ThrowingActionFilter]
        public string ThrowingActionFilter()
        {
            return "Throwing Action Filter";
        }

        [ThrowingExceptionFilter]
        public string ThrowingExceptionFilter()
        {
            return "Throwing Exception Filter";
        }

        [ThrowingAuthorizationFilter]
        public string ThrowingAuthorizationFilter()
        {
            return "Throwing Authorization Filter";
        }

        [HandleInvalidOperationExceptionFilter]
        [ShortCircuitExceptionFilter(Order=12)]
        public IActionResult ThrowRandomExcpetion()
        {
            throw new InvalidOperationException("Controller threw.");
        }
    }
}