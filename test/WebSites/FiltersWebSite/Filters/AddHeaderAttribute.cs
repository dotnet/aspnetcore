// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Mvc.Filters;

namespace FiltersWebSite
{
    public class AddHeaderAttribute : ResultFilterAttribute
    {
        public override void OnResultExecuted(ResultExecutedContext context)
        {
            context.HttpContext.Response.Headers.Add(
                "OnResultExecuted", new string[] { "ResultExecutedSuccessfully" });

            base.OnResultExecuted(context);
        }
    }
}