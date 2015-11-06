// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.AspNet.Cors.Infrastructure;
using Microsoft.AspNet.Http;
using Microsoft.AspNet.Mvc.Filters;
using Microsoft.Extensions.Primitives;

namespace Microsoft.AspNet.Mvc.Cors
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
        public Task OnAuthorizationAsync(AuthorizationContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            var accessControlRequestMethod =
                        context.HttpContext.Request.Headers[CorsConstants.AccessControlRequestMethod];
            if (string.Equals(
                    context.HttpContext.Request.Method,
                    CorsConstants.PreflightHttpMethod,
                    StringComparison.Ordinal) &&
                !StringValues.IsNullOrEmpty(accessControlRequestMethod))
            {
                // Short circuit if the request is preflight as that should not result in action execution.
                context.Result = new HttpStatusCodeResult(StatusCodes.Status200OK);
            }

            // Let the action be executed.
            return Task.FromResult(true);
        }
    }
}
