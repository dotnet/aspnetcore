// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Mvc
{
    /// <summary>
    /// A <see cref="ActionResult"/> that when executed will produce a response with content.
    /// </summary>
    public class ContentResult : ActionResult, IResult, IStatusCodeActionResult
    {
        private const string DefaultContentType = "text/plain; charset=utf-8";

        /// <summary>
        /// Gets or set the content representing the body of the response.
        /// </summary>
        public string? Content { get; set; }

        /// <summary>
        /// Gets or sets the Content-Type header for the response.
        /// </summary>
        public string? ContentType { get; set; }

        /// <summary>
        /// Gets or sets the HTTP status code.
        /// </summary>
        public int? StatusCode { get; set; }

        /// <inheritdoc />
        public override Task ExecuteResultAsync(ActionContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            var executor = context.HttpContext.RequestServices.GetRequiredService<IActionResultExecutor<ContentResult>>();
            return executor.ExecuteAsync(context, this);
        }

        /// <summary>
        /// Writes the content to the HTTP response.
        /// </summary>
        /// <param name="httpContext">The <see cref="HttpContext"/> for the current request.</param>
        /// <returns>A task that represents the asynchronous execute operation.</returns>
        async Task IResult.ExecuteAsync(HttpContext httpContext)
        {
            if (httpContext == null)
            {
                throw new ArgumentNullException(nameof(httpContext));
            }

            var response = httpContext.Response;

            ResponseContentTypeHelper.ResolveContentTypeAndEncoding(
                ContentType,
                response.ContentType,
                DefaultContentType,
                out var resolvedContentType,
                out var resolvedContentTypeEncoding);

            response.ContentType = resolvedContentType;

            if (StatusCode != null)
            {
                response.StatusCode = StatusCode.Value;
            }

            var factory = httpContext.RequestServices.GetRequiredService<ILoggerFactory>();
            var logger = factory.CreateLogger<ContentResult>();

            logger.ContentResultExecuting(resolvedContentType);

            if (Content != null)
            {
                response.ContentLength = resolvedContentTypeEncoding.GetByteCount(Content);
                await response.WriteAsync(Content, resolvedContentTypeEncoding);
            }
        }
    }
}
