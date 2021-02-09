// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Api;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Mvc
{
    /// <summary>
    /// Represents an <see cref="ActionResult"/> that when executed will
    /// produce an HTTP response with the given response status code.
    /// </summary>
    public class StatusCodeResult : ActionResult, IResult, IClientErrorActionResult
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

            Execute(context.HttpContext);
        }

        /// <summary>
        /// Sets the status code on the HTTP response.
        /// </summary>
        /// <param name="httpContext">The <see cref="HttpContext"/> for the current request.</param>
        /// <returns>A task that represents the asynchronous execute operation.</returns>
        Task IResult.ExecuteAsync(HttpContext httpContext)
        {
            Execute(httpContext);
            return Task.CompletedTask;
        }

        private void Execute(HttpContext httpContext)
        {
            var factory = httpContext.RequestServices.GetRequiredService<ILoggerFactory>();
            var logger = factory.CreateLogger<StatusCodeResult>();

            logger.HttpStatusCodeResultExecuting(StatusCode);

            httpContext.Response.StatusCode = StatusCode;
        }
    }
}
