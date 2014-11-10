// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.AspNet.Mvc;

namespace FormatterWebSite
{
    public class CustomObjectResult : ObjectResult
    {
        public CustomObjectResult(object value, int statusCode) : base(value)
        {
            StatusCode = statusCode;
        }

        public int StatusCode { get; private set; }

        public override Task ExecuteResultAsync(ActionContext context)
        {
            context.HttpContext.Response.StatusCode = StatusCode;

            return base.ExecuteResultAsync(context);
        }
    }
}