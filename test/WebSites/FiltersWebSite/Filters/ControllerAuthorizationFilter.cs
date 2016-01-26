// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Filters;

namespace FiltersWebSite
{
    public class ControllerAuthorizationFilter : AuthorizeUserAttribute
    {
        public override void OnAuthorization(AuthorizationFilterContext context)
        {
            context.HttpContext.Response.Headers.Append("filters", "On Controller Authorization Filter - OnAuthorization");
        }
    }
}