// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.Internal;
using Microsoft.Extensions.Primitives;

namespace Microsoft.AspNetCore.Mvc.Cors.Internal
{
    /// <summary>
    /// An <see cref="ICorsAuthorizationFilter"/> which ensures that an action does not run for a pre-flight request.
    /// </summary>
    public class DisableCorsAuthorizationFilter : ICorsAuthorizationFilter
    {
        /// <inheritdoc />
        public int Order
        {
            get
            {
                // Since clients' preflight requests would not have data to authenticate requests, this
                // filter must run before any other authorization filters.
                return int.MinValue + 100;
            }
        }

        /// <inheritdoc />
        public Task OnAuthorizationAsync(AuthorizationFilterContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            var accessControlRequestMethod =
                        context.HttpContext.Request.Headers[CorsConstants.AccessControlRequestMethod];
            if (string.Equals(
                    context.HttpContext.Request.Method,
                    CorsConstants.PreflightHttpMethod,
                    StringComparison.OrdinalIgnoreCase) &&
                !StringValues.IsNullOrEmpty(accessControlRequestMethod))
            {
                // Short circuit if the request is preflight as that should not result in action execution.
                context.Result = new StatusCodeResult(StatusCodes.Status200OK);
            }

            // Let the action be executed.
            return TaskCache.CompletedTask;
        }
    }
}
