// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNet.Mvc;
using System.Security.Claims;

namespace FiltersWebSite
{
    public class AuthorizeUserAttribute : AuthorizationFilterAttribute
    {
        public override void OnAuthorization(AuthorizationContext context)
        {
            if (context.ActionDescriptor.DisplayName == "FiltersWebSite.ProductsController.GetPrice")
            {
                context.HttpContext.Response.Headers.Append("filters",
                    "Authorize Filter On Action - OnAuthorization");
            }

            context.HttpContext.User = new ClaimsPrincipal(
                new ClaimsIdentity(
                    new Claim[] { 
                        new Claim("Permission", "CanViewPage"),
                        new Claim(ClaimTypes.Role, "Administrator"), 
                        new Claim(ClaimTypes.NameIdentifier, "John")}, 
                        "Basic"));
        }
    }
}
