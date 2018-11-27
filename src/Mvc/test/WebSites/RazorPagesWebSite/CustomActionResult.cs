// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace RazorPagesWebSite
{
    public class CustomActionResult : IActionResult
    {
        public Task ExecuteResultAsync(ActionContext context)
        {
            context.HttpContext.Response.StatusCode = 200;
            return context.HttpContext.Response.WriteAsync(nameof(CustomActionResult));
        }
    }
}
