// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Mvc;

namespace FiltersWebSite
{
    [ControllerExceptionFilter]
    public class ExceptionOrderController : Controller
    {
        [HandleInvalidOperationExceptionFilter]
        public string GetError(string error)
        {
            throw new InvalidOperationException(error);
        }
    }
}