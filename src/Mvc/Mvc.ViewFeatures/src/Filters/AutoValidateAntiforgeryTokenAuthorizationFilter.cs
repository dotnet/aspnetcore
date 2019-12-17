// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Mvc.ViewFeatures.Filters
{
    internal class AutoValidateAntiforgeryTokenAuthorizationFilter : ValidateAntiforgeryTokenAuthorizationFilter
    {
        public AutoValidateAntiforgeryTokenAuthorizationFilter(IAntiforgery antiforgery, ILoggerFactory loggerFactory)
            : base(antiforgery, loggerFactory)
        {
        }

        protected override bool ShouldValidate(AuthorizationFilterContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            var method = context.HttpContext.Request.Method;
            if (HttpMethods.IsGet(method) ||
                HttpMethods.IsHead(method) ||
                HttpMethods.IsTrace(method) ||
                HttpMethods.IsOptions(method))
            {
                return false;
            }

            // Anything else requires a token.
            return true;
        }
    }
}
