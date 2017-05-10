// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Mvc;

namespace Microsoft.AspNetCore.Identity.Service.Mvc
{
    public static class UrlHelperExtensions
    {
        public static string GenerateEndpointLink(
            this IUrlHelper urlHelper,
            string action,
            string controller,
            object values,
            string host)
        {
            var httpContext = urlHelper.ActionContext.HttpContext;
            return urlHelper.Action(
                action,
                controller,
                values,
                httpContext.Request.Scheme,
                host ?? httpContext.Request.Host.ToString());
        }
    }
}
