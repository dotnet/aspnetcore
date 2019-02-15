// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Mvc
{
    /// <summary>
    /// Represents an <see cref="ActionResult"/> that when executed will
    /// produce an HTTP response with the given response status code.
    /// </summary>
    public class StatusCodeResult : ActionResult, IClientErrorActionResult
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="StatusCodeResult"/> class
        /// with the given <paramref name="statusCode"/>.
        /// </summary>
        /// <param name="statusCode">The HTTP status code of the response.</param>
        public StatusCodeResult([ActionResultStatusCode] int statusCode)
        {
            StatusCode = statusCode;
        }

        /// <summary>
        /// Gets the HTTP status code.
        /// </summary>
        public int StatusCode { get; }

        int? IStatusCodeActionResult.StatusCode => StatusCode;

        /// <inheritdoc />
        public override void ExecuteResult(ActionContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            var factory = context.HttpContext.RequestServices.GetRequiredService<ILoggerFactory>();
            var logger = factory.CreateLogger<StatusCodeResult>();

            logger.HttpStatusCodeResultExecuting(StatusCode);

            context.HttpContext.Response.StatusCode = StatusCode;
        }
    }
}
