// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNet.Http;
using Microsoft.AspNet.Mvc.Filters;

namespace FiltersWebSite
{
    public class ControllerAuthorizationFilter : AuthorizeUserAttribute
    {
        public override void OnAuthorization(AuthorizationContext context)
        {
            context.HttpContext.Response.Headers.Append("filters", "On Controller Authorization Filter - OnAuthorization");
        }
    }
}