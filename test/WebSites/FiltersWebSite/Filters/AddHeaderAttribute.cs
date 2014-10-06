// Copyright(c) Microsoft Open Technologies, Inc.All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Text;
using Microsoft.AspNet.Mvc;

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